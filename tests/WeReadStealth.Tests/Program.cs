using System;
using System.IO;
using WeReadStealth;

internal static class Program
{
    private static int Main()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), "WeReadStealth.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(testDirectory);
            TestDefaults();
            TestNormalization();
            TestSettingsMigration();
            TestToolbarVisibilityPolicy();
            TestWebViewEnvironment();
            TestWebViewPageBridge();
            TestResizableWindowStyle();
            TestRoundTrip(testDirectory);
            TestCorruptFileRecovery(testDirectory);
            Console.WriteLine("All settings tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    private static void TestDefaults()
    {
        AppSettings settings = AppSettings.CreateDefault();
        AssertEqual(400, settings.Width, "default width");
        AssertEqual(600, settings.Height, "default height");
        AssertEqual(1.0, settings.Opacity, "default opacity");
        AssertEqual(true, settings.AlwaysOnTop, "default top-most state");
        AssertEqual("light", settings.Theme, "default theme");
        AssertEqual(true, settings.AutoHideToolbar, "default toolbar auto-hide");
        AssertEqual(AppSettings.CurrentVersion, settings.Version, "default settings version");
    }

    private static void TestNormalization()
    {
        AppSettings settings = new AppSettings
        {
            Width = 10,
            Height = 10,
            Opacity = 5.0,
            Theme = "unknown"
        };

        settings.Normalize();
        AssertEqual(AppSettings.MinimumWidth, settings.Width, "minimum width");
        AssertEqual(AppSettings.MinimumHeight, settings.Height, "minimum height");
        AssertEqual(1.0, settings.Opacity, "maximum opacity");
        AssertEqual("light", settings.Theme, "invalid theme fallback");

        settings.Opacity = 0.1;
        settings.Normalize();
        AssertEqual(0.3, settings.Opacity, "minimum opacity");
    }

    private static void TestRoundTrip(string testDirectory)
    {
        string path = Path.Combine(testDirectory, "round-trip.json");
        SettingsStore store = new SettingsStore(path);
        AppSettings expected = new AppSettings
        {
            X = 120,
            Y = 240,
            Width = 720,
            Height = 880,
            Opacity = 0.72,
            AlwaysOnTop = false,
            Theme = "dark",
            AutoHideToolbar = false,
            Version = AppSettings.CurrentVersion
        };

        store.Save(expected);
        AppSettings actual = store.Load();

        AssertEqual(expected.X, actual.X, "saved x");
        AssertEqual(expected.Y, actual.Y, "saved y");
        AssertEqual(expected.Width, actual.Width, "saved width");
        AssertEqual(expected.Height, actual.Height, "saved height");
        AssertEqual(expected.Opacity, actual.Opacity, "saved opacity");
        AssertEqual(expected.AlwaysOnTop, actual.AlwaysOnTop, "saved top-most state");
        AssertEqual(expected.Theme, actual.Theme, "saved theme");
        AssertEqual(expected.AutoHideToolbar, actual.AutoHideToolbar, "saved toolbar auto-hide");
        AssertEqual(expected.Version, actual.Version, "saved settings version");
    }

    private static void TestSettingsMigration()
    {
        AppSettings legacy = new AppSettings
        {
            Width = 400,
            Height = 600,
            Opacity = 1.0,
            Theme = "dark"
        };

        legacy.Normalize();
        AssertEqual(true, legacy.AutoHideToolbar, "legacy toolbar auto-hide migration");
        AssertEqual(AppSettings.CurrentVersion, legacy.Version, "legacy settings version migration");

        legacy.AutoHideToolbar = false;
        legacy.Normalize();
        AssertEqual(false, legacy.AutoHideToolbar, "explicit disabled toolbar auto-hide");
    }

    private static void TestToolbarVisibilityPolicy()
    {
        AssertEqual(true, ToolbarVisibilityPolicy.ShouldShow(false, false, false, 100, TimeSpan.FromSeconds(10)), "disabled auto-hide");
        AssertEqual(true, ToolbarVisibilityPolicy.ShouldShow(true, true, false, 100, TimeSpan.FromMilliseconds(500)), "startup grace period");
        AssertEqual(false, ToolbarVisibilityPolicy.ShouldShow(true, true, false, 100, TimeSpan.FromSeconds(2)), "hide after delay");
        AssertEqual(true, ToolbarVisibilityPolicy.ShouldShow(true, false, true, 4, TimeSpan.FromSeconds(2)), "top-edge reveal");
        AssertEqual(true, ToolbarVisibilityPolicy.ShouldShow(true, true, true, 30, TimeSpan.FromSeconds(2)), "toolbar hover retention");
        AssertEqual(false, ToolbarVisibilityPolicy.ShouldShow(true, true, true, 80, TimeSpan.FromSeconds(2)), "hide below toolbar");
    }

    private static void TestWebViewEnvironment()
    {
        AssertEqual(
            Microsoft.Web.WebView2.Core.CoreWebView2ScrollbarStyle.Default,
            WebViewEnvironment.CreateOptions().ScrollBarStyle,
            "WebView2 default scrollbar style");
        AssertEqual(
            "--hide-scrollbars",
            WebViewEnvironment.CreateOptions().AdditionalBrowserArguments,
            "WebView2 hidden scrollbar argument");
    }

    private static void TestWebViewPageBridge()
    {
        AssertEqual(true, WebViewPageBridge.TryParseBackgroundMessage("wrs-bg:#1d1d1d", out System.Drawing.Color dark), "valid page background message");
        AssertEqual(System.Drawing.Color.FromArgb(29, 29, 29), dark, "parsed page background color");
        AssertEqual(false, WebViewPageBridge.TryParseBackgroundMessage("wrs-bg:red", out _), "invalid page background message");
        AssertEqual(false, WebViewPageBridge.TryParseBackgroundMessage("other:#ffffff", out _), "untrusted page message");
    }

    private static void TestResizableWindowStyle()
    {
        int style = WindowStyles.AddResizable(0);
        AssertEqual(NativeMethods.WsThickFrame, style & NativeMethods.WsThickFrame, "resizable window style");
    }

    private static void TestCorruptFileRecovery(string testDirectory)
    {
        string path = Path.Combine(testDirectory, "corrupt.json");
        File.WriteAllText(path, "{ this is not json");

        AppSettings settings = new SettingsStore(path).Load();

        AssertEqual(400, settings.Width, "corrupt file default width");
        AssertEqual(1.0, settings.Opacity, "corrupt file default opacity");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"Assertion failed for {name}: expected {expected}, actual {actual}.");
        }
    }
}
