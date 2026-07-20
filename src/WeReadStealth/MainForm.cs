using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WeReadStealth
{
    internal sealed class MainForm : Form
    {
        private const int ControlBarHeight = 36;
        private const int ResizeBorderWidth = 6;
        private static readonly Uri HomeUri = new Uri("https://weread.qq.com/");

        private readonly SettingsStore _settingsStore;
        private readonly AppSettings _settings;
        private readonly WebView2 _webView;
        private readonly Panel _controlBar;
        private readonly Label _titleLabel;
        private readonly Button _themeButton;
        private readonly Button _pinButton;
        private readonly TrackBar _opacitySlider;
        private readonly Label _opacityLabel;
        private readonly Button _hideButton;
        private readonly Button _closeButton;
        private readonly Panel _errorPanel;
        private readonly Label _errorLabel;
        private readonly Button _retryButton;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly Timer _saveTimer;
        private readonly ToolTip _toolTip;

        private HotkeyManager _hotkeyManager;
        private Icon _applicationIcon;
        private bool _exitRequested;
        private bool _webViewReady;

        internal MainForm(SettingsStore settingsStore)
        {
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _settings = _settingsStore.Load();

            Text = "WeRead Stealth";
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(AppSettings.MinimumWidth, AppSettings.MinimumHeight);
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            KeyPreview = true;
            TopMost = _settings.AlwaysOnTop;
            Opacity = _settings.Opacity;
            SetInitialBounds();

            _applicationIcon = LoadApplicationIcon();
            if (_applicationIcon != null)
            {
                Icon = _applicationIcon;
            }

            _toolTip = new ToolTip
            {
                AutomaticDelay = 350,
                AutoPopDelay = 6000,
                ShowAlways = true
            };

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.White
            };

            _controlBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = ControlBarHeight,
                Padding = new Padding(6, 2, 2, 2)
            };

            TableLayoutPanel toolbarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Text = "微信读书",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Margin = new Padding(4, 0, 2, 0)
            };

            _themeButton = CreateToolbarButton("◐", "切换主题 (Ctrl+Shift+M)", "切换浅色或深色工具栏主题");
            _pinButton = CreateToolbarButton("⌖", "置顶 (Ctrl+Shift+T)", "切换窗口置顶");
            _hideButton = CreateToolbarButton("−", "隐藏 (Ctrl+Shift+W)", "隐藏到系统托盘");
            _closeButton = CreateToolbarButton("×", "关闭到托盘 (Ctrl+Shift+B)", "关闭按钮会将窗口隐藏到系统托盘");

            Panel opacityPanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            _opacitySlider = new TrackBar
            {
                Minimum = 30,
                Maximum = 100,
                TickStyle = TickStyle.None,
                SmallChange = 2,
                LargeChange = 10,
                Value = (int)Math.Round(_settings.Opacity * 100),
                Width = 68,
                Height = 30,
                Location = new Point(0, 1),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _opacityLabel = new Label
            {
                AutoSize = false,
                Width = 34,
                Height = 30,
                Location = new Point(69, 1),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = _opacitySlider.Value + "%",
                Font = new Font("Segoe UI", 8F)
            };
            opacityPanel.Controls.Add(_opacitySlider);
            opacityPanel.Controls.Add(_opacityLabel);
            _toolTip.SetToolTip(opacityPanel, "调整整个窗口透明度");
            _toolTip.SetToolTip(_opacitySlider, "调整整个窗口透明度");

            toolbarLayout.Controls.Add(_titleLabel, 0, 0);
            toolbarLayout.Controls.Add(_themeButton, 1, 0);
            toolbarLayout.Controls.Add(_pinButton, 2, 0);
            toolbarLayout.Controls.Add(opacityPanel, 3, 0);
            toolbarLayout.Controls.Add(_hideButton, 4, 0);
            toolbarLayout.Controls.Add(_closeButton, 5, 0);
            _controlBar.Controls.Add(toolbarLayout);

            _errorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(24)
            };
            _errorLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F),
                AutoEllipsis = true
            };
            _retryButton = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                Text = "重试",
                FlatStyle = FlatStyle.Flat
            };
            _errorPanel.Controls.Add(_errorLabel);
            _errorPanel.Controls.Add(_retryButton);

            Controls.Add(_webView);
            Controls.Add(_errorPanel);
            Controls.Add(_controlBar);

            _trayMenu = new ContextMenuStrip();
            _trayIcon = new NotifyIcon
            {
                Text = "WeRead Stealth",
                Icon = _applicationIcon ?? SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = _trayMenu
            };

            _saveTimer = new Timer { Interval = 400 };
            _saveTimer.Tick += SaveTimerOnTick;

            _themeButton.Click += delegate { ToggleTheme(); };
            _pinButton.Click += delegate { ToggleTopMost(); };
            _hideButton.Click += delegate { HideToTray(); };
            _closeButton.Click += delegate { HideToTray(); };
            _opacitySlider.ValueChanged += OpacitySliderOnValueChanged;
            _retryButton.Click += async delegate { await InitializeOrReloadWebViewAsync(); };
            _trayIcon.DoubleClick += delegate { ShowFromTray(); };
            _titleLabel.MouseDown += DragWindow;
            _controlBar.MouseDown += DragWindow;

            LocationChanged += delegate { ScheduleSettingsSave(); };
            SizeChanged += delegate { ScheduleSettingsSave(); };
            FormClosing += MainFormOnFormClosing;
            Shown += MainFormOnShown;

            ApplyTheme();
            UpdatePinState();
            RebuildTrayMenu();
        }

        internal void ShowFromTray()
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowFromTray));
                return;
            }

            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            NativeMethods.SetForegroundWindow(Handle);
            RebuildTrayMenu();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.Style = WindowStyles.AddResizable(parameters.Style);
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (_hotkeyManager != null && _hotkeyManager.TryGetHotkey(message, out AppHotkey hotkey))
            {
                HandleHotkey(hotkey);
                return;
            }

            if (message.Msg == NativeMethods.WmNcHitTest)
            {
                base.WndProc(ref message);
                if (message.Result.ToInt32() == NativeMethods.HtClient)
                {
                    Point clientPoint = PointToClient(GetScreenPoint(message.LParam));
                    int hit = GetResizeHit(clientPoint);
                    if (hit != NativeMethods.HtClient)
                    {
                        message.Result = new IntPtr(hit);
                        return;
                    }
                }

                return;
            }

            base.WndProc(ref message);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _saveTimer?.Dispose();
                _hotkeyManager?.Dispose();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayMenu.Dispose();
                _toolTip.Dispose();
                _webView.Dispose();
                _applicationIcon?.Dispose();
            }

            base.Dispose(disposing);
        }

        private async void MainFormOnShown(object sender, EventArgs eventArgs)
        {
            _hotkeyManager = new HotkeyManager(Handle);
            IReadOnlyList<AppHotkey> conflicts = _hotkeyManager.RegisterDefaults();
            if (conflicts.Count > 0)
            {
                string names = string.Join(", ", conflicts.Select(GetHotkeyText));
                _trayIcon.ShowBalloonTip(5000, "部分快捷键不可用", names + " 已被其他程序占用。", ToolTipIcon.Warning);
            }

            RebuildTrayMenu();
            await InitializeOrReloadWebViewAsync();
        }

        private async Task InitializeOrReloadWebViewAsync()
        {
            HideError();

            try
            {
                if (!_webViewReady)
                {
                    string userDataFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "WeReadStealth",
                        "WebView2");
                    CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    await _webView.EnsureCoreWebView2Async(environment);
                    ConfigureWebView();
                    _webViewReady = true;
                }

                _webView.CoreWebView2.Navigate(HomeUri.AbsoluteUri);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                ShowError("未检测到 Microsoft Edge WebView2 Runtime。请安装运行时后重试。", true);
            }
            catch (Exception exception)
            {
                ShowError("WebView2 初始化失败：" + exception.Message, true);
            }
        }

        private void ConfigureWebView()
        {
            CoreWebView2Settings settings = _webView.CoreWebView2.Settings;
            settings.AreDevToolsEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = true;
            settings.AreDefaultScriptDialogsEnabled = true;
            settings.AreDefaultContextMenusEnabled = true;

            _webView.CoreWebView2.NavigationStarting += delegate (object sender, CoreWebView2NavigationStartingEventArgs args)
            {
                if (!IsSafeWebUri(args.Uri))
                {
                    args.Cancel = true;
                    OpenExternal(args.Uri);
                }
            };

            _webView.CoreWebView2.NavigationCompleted += delegate (object sender, CoreWebView2NavigationCompletedEventArgs args)
            {
                if (args.IsSuccess)
                {
                    HideError();
                }
                else
                {
                    ShowError("微信读书加载失败，请检查网络后重试。错误：" + args.WebErrorStatus, true);
                }
            };

            _webView.CoreWebView2.NewWindowRequested += delegate (object sender, CoreWebView2NewWindowRequestedEventArgs args)
            {
                args.Handled = true;
                if (IsSafeWebUri(args.Uri))
                {
                    _webView.CoreWebView2.Navigate(args.Uri);
                }
                else
                {
                    OpenExternal(args.Uri);
                }
            };

            _webView.CoreWebView2.ProcessFailed += delegate
            {
                ShowError("网页进程意外退出，请点击重试。", true);
            };
        }

        private void HandleHotkey(AppHotkey hotkey)
        {
            switch (hotkey)
            {
                case AppHotkey.ToggleVisibility:
                    if (Visible)
                    {
                        HideToTray();
                    }
                    else
                    {
                        ShowFromTray();
                    }
                    break;
                case AppHotkey.HideImmediately:
                    HideToTray();
                    break;
                case AppHotkey.ToggleTopMost:
                    ToggleTopMost();
                    break;
                case AppHotkey.CycleOpacity:
                    CycleOpacity();
                    break;
                case AppHotkey.ToggleTheme:
                    ToggleTheme();
                    break;
            }
        }

        private void ToggleTopMost()
        {
            _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
            TopMost = _settings.AlwaysOnTop;
            UpdatePinState();
            RebuildTrayMenu();
            ScheduleSettingsSave();
        }

        private void ToggleTheme()
        {
            _settings.Theme = _settings.Theme == "dark" ? "light" : "dark";
            ApplyTheme();
            RebuildTrayMenu();
            ScheduleSettingsSave();
        }

        private void CycleOpacity()
        {
            int nextValue;
            if (_opacitySlider.Value > 90)
            {
                nextValue = 80;
            }
            else if (_opacitySlider.Value > 70)
            {
                nextValue = 60;
            }
            else
            {
                nextValue = 100;
            }

            _opacitySlider.Value = nextValue;
        }

        private void OpacitySliderOnValueChanged(object sender, EventArgs eventArgs)
        {
            _settings.Opacity = _opacitySlider.Value / 100.0;
            Opacity = _settings.Opacity;
            _opacityLabel.Text = _opacitySlider.Value + "%";
            ScheduleSettingsSave();
        }

        private void ApplyTheme()
        {
            UiTheme theme = _settings.Theme == "dark" ? UiTheme.Dark : UiTheme.Light;
            _controlBar.BackColor = theme.Background;
            _controlBar.ForeColor = theme.Foreground;
            _titleLabel.ForeColor = theme.Foreground;
            _opacityLabel.ForeColor = theme.Foreground;
            _errorPanel.BackColor = theme.Background;
            _errorPanel.ForeColor = theme.Foreground;
            _errorLabel.ForeColor = theme.Foreground;

            foreach (Button button in new[] { _themeButton, _pinButton, _hideButton, _closeButton, _retryButton })
            {
                button.BackColor = theme.Background;
                button.ForeColor = theme.Foreground;
                button.FlatAppearance.BorderColor = theme.Border;
                button.FlatAppearance.MouseOverBackColor = theme.Border;
            }

            _pinButton.ForeColor = _settings.AlwaysOnTop ? theme.Accent : theme.Foreground;
            _closeButton.ForeColor = theme.Danger;
            _retryButton.ForeColor = theme.Accent;
            BackColor = theme.Border;
        }

        private void UpdatePinState()
        {
            _pinButton.Text = _settings.AlwaysOnTop ? "●" : "○";
            _toolTip.SetToolTip(_pinButton, _settings.AlwaysOnTop ? "已置顶，点击取消 (Ctrl+Shift+T)" : "点击置顶 (Ctrl+Shift+T)");
            if (_controlBar != null)
            {
                ApplyTheme();
            }
        }

        private void RebuildTrayMenu()
        {
            _trayMenu.Items.Clear();
            _trayMenu.Items.Add(Visible ? "隐藏" : "显示", null, delegate
            {
                if (Visible) HideToTray(); else ShowFromTray();
            });
            ToolStripMenuItem topMostItem = new ToolStripMenuItem("置顶", null, delegate { ToggleTopMost(); })
            {
                Checked = _settings.AlwaysOnTop
            };
            _trayMenu.Items.Add(topMostItem);
            _trayMenu.Items.Add(_settings.Theme == "dark" ? "浅色主题" : "深色主题", null, delegate { ToggleTheme(); });
            _trayMenu.Items.Add("返回微信读书首页", null, delegate
            {
                ShowFromTray();
                if (_webViewReady) _webView.CoreWebView2.Navigate(HomeUri.AbsoluteUri);
            });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("退出", null, delegate { ExitApplication(); });
        }

        private void HideToTray()
        {
            Hide();
            RebuildTrayMenu();
        }

        private void ExitApplication()
        {
            SaveSettingsNow();
            _exitRequested = true;
            _trayIcon.Visible = false;
            Close();
        }

        private void MainFormOnFormClosing(object sender, FormClosingEventArgs eventArgs)
        {
            if (!_exitRequested && eventArgs.CloseReason == CloseReason.UserClosing)
            {
                eventArgs.Cancel = true;
                HideToTray();
                return;
            }

            SaveSettingsNow();
        }

        private void ScheduleSettingsSave()
        {
            if (_exitRequested || WindowState == FormWindowState.Minimized)
            {
                return;
            }

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void SaveTimerOnTick(object sender, EventArgs eventArgs)
        {
            _saveTimer.Stop();
            SaveSettingsNow();
        }

        private void SaveSettingsNow()
        {
            if (WindowState == FormWindowState.Normal)
            {
                _settings.X = Bounds.X;
                _settings.Y = Bounds.Y;
                _settings.Width = Bounds.Width;
                _settings.Height = Bounds.Height;
            }

            try
            {
                _settingsStore.Save(_settings);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to save settings: " + exception);
            }
        }

        private void SetInitialBounds()
        {
            Rectangle bounds = new Rectangle(
                _settings.X ?? 100,
                _settings.Y ?? 100,
                _settings.Width,
                _settings.Height);

            bool intersectsScreen = Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(bounds));
            if (_settings.X.HasValue && _settings.Y.HasValue && intersectsScreen)
            {
                Bounds = bounds;
            }
            else
            {
                Size = new Size(_settings.Width, _settings.Height);
                StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private Button CreateToolbarButton(string text, string accessibleName, string tooltip)
        {
            Button button = new Button
            {
                Dock = DockStyle.Fill,
                Text = text,
                AccessibleName = accessibleName,
                TabStop = false,
                FlatStyle = FlatStyle.Flat,
                Margin = Padding.Empty,
                Font = new Font("Segoe UI Symbol", 11F),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            _toolTip.SetToolTip(button, tooltip);
            return button;
        }

        private void DragWindow(object sender, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button != MouseButtons.Left)
            {
                return;
            }

            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WmNcLButtonDown, new IntPtr(NativeMethods.HtCaption), IntPtr.Zero);
        }

        private int GetResizeHit(Point point)
        {
            bool left = point.X <= ResizeBorderWidth;
            bool right = point.X >= ClientSize.Width - ResizeBorderWidth;
            bool top = point.Y <= ResizeBorderWidth;
            bool bottom = point.Y >= ClientSize.Height - ResizeBorderWidth;

            if (left && top) return NativeMethods.HtTopLeft;
            if (right && top) return NativeMethods.HtTopRight;
            if (left && bottom) return NativeMethods.HtBottomLeft;
            if (right && bottom) return NativeMethods.HtBottomRight;
            if (left) return NativeMethods.HtLeft;
            if (right) return NativeMethods.HtRight;
            if (top) return NativeMethods.HtTop;
            if (bottom) return NativeMethods.HtBottom;
            return NativeMethods.HtClient;
        }

        private static Point GetScreenPoint(IntPtr lParam)
        {
            long value = lParam.ToInt64();
            int x = unchecked((short)(value & 0xffff));
            int y = unchecked((short)((value >> 16) & 0xffff));
            return new Point(x, y);
        }

        private static bool IsSafeWebUri(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            return string.Equals(uri.Host, "qq.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".qq.com", StringComparison.OrdinalIgnoreCase);
        }

        private static void OpenExternal(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
            {
                return;
            }

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to open external URI: " + exception);
            }
        }

        private void ShowError(string message, bool allowRetry)
        {
            _errorLabel.Text = message;
            _retryButton.Visible = allowRetry;
            _errorPanel.Visible = true;
            _errorPanel.BringToFront();
            _controlBar.BringToFront();
        }

        private void HideError()
        {
            _errorPanel.Visible = false;
        }

        private Icon LoadApplicationIcon()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.png");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using (Bitmap bitmap = new Bitmap(path))
                {
                    IntPtr handle = bitmap.GetHicon();
                    try
                    {
                        using (Icon icon = Icon.FromHandle(handle))
                        {
                            return (Icon)icon.Clone();
                        }
                    }
                    finally
                    {
                        NativeMethods.DestroyIcon(handle);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to load application icon: " + exception);
                return null;
            }
        }

        private static string GetHotkeyText(AppHotkey hotkey)
        {
            switch (hotkey)
            {
                case AppHotkey.ToggleVisibility: return "Ctrl+Shift+W";
                case AppHotkey.HideImmediately: return "Ctrl+Shift+B";
                case AppHotkey.ToggleTopMost: return "Ctrl+Shift+T";
                case AppHotkey.CycleOpacity: return "Ctrl+Shift+O";
                case AppHotkey.ToggleTheme: return "Ctrl+Shift+M";
                default: return hotkey.ToString();
            }
        }
    }
}
