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
            Theme = "dark"
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
