using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

using LIveSubs.utils;
using LIveSubs.Utils;
using Button = Wpf.Ui.Controls.Button;

namespace LIveSubs
{
    public partial class MainWindow : FluentWindow
    {
        public OverlayWindow? OverlayWindow { get; set; } = null;
        public bool IsAutoHeight { get; set; } = true;
        public bool IsCompactMode { get; set; } = false;

        private HotkeyManager? _hotkeyManager;
        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private bool _isExiting = false;

        // Store normal window dimensions for compact mode toggle
        private double _normalWidth;
        private double _normalHeight;
        private double _normalMinWidth;
        private double _normalMinHeight;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                RootNavigation.Navigate(typeof(CaptionPage));
                IsAutoHeight = true;
                CheckForFirstUse();
                CheckForUpdates();

                InitializeHotkeys();
                InitializeTrayIcon();
            };

            Closing += MainWindow_Closing;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            var windowState = WindowHandler.LoadState(this, Translator.Setting);
            if (windowState.Left <= 0 || windowState.Left >= screenWidth ||
                windowState.Top <= 0 || windowState.Top >= screenHeight)
            {
                WindowHandler.RestoreState(this, new Rect(
                    (screenWidth - 775) / 2, screenHeight * 3 / 4 - 167, 775, 167));
            }
            else
                WindowHandler.RestoreState(this, windowState);

            ToggleTopmost(Translator.Setting.MainWindow.Topmost);
            ShowLogCard(Translator.Setting.MainWindow.CaptionLogEnabled);
        }

        #region System Tray

        private void InitializeTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();

            // Load the app icon
            string iconPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "src", "LiveCaptions-Translator.ico");
            if (File.Exists(iconPath))
                _trayIcon.Icon = new Icon(iconPath);
            else
                _trayIcon.Icon = SystemIcons.Application;

            _trayIcon.Text = "LiveCaptions Translator";
            _trayIcon.Visible = true;

            // Double-click to show/hide
            _trayIcon.DoubleClick += (s, e) => ToggleWindowVisibility();

            // Context menu
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var showItem = new System.Windows.Forms.ToolStripMenuItem("Show / Hide");
            showItem.Click += (s, e) => ToggleWindowVisibility();
            showItem.Font = new Font(showItem.Font, System.Drawing.FontStyle.Bold);
            contextMenu.Items.Add(showItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var pauseItem = new System.Windows.Forms.ToolStripMenuItem("Toggle Pause");
            pauseItem.Click += (s, e) => Dispatcher.Invoke(() => TogglePause());
            contextMenu.Items.Add(pauseItem);

            var overlayItem = new System.Windows.Forms.ToolStripMenuItem("Toggle Overlay");
            overlayItem.Click += (s, e) => Dispatcher.Invoke(() => OverlayModeButton_Click(OverlayModeButton, new RoutedEventArgs()));
            contextMenu.Items.Add(overlayItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) =>
            {
                _isExiting = true;
                Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
            };
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;
        }

        private void ToggleWindowVisibility()
        {
            Dispatcher.Invoke(() =>
            {
                if (this.IsVisible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
            });
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExiting)
            {
                // Minimize to tray instead of closing
                e.Cancel = true;
                this.Hide();
                _trayIcon?.ShowBalloonTip(2000, "LiveCaptions Translator",
                    "Application minimized to tray. Double-click to restore.",
                    System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                // Actually closing — cleanup
                _hotkeyManager?.Dispose();
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
            }
        }

        #endregion

        #region Global Hotkeys

        private void InitializeHotkeys()
        {
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Initialize(this);

            // Ctrl+Shift+P — Toggle Pause
            _hotkeyManager.RegisterGlobalHotkey(
                HotkeyManager.HOTKEY_TOGGLE_PAUSE,
                HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT,
                HotkeyManager.VK_P,
                () => Dispatcher.Invoke(TogglePause));

            // Ctrl+Shift+O — Toggle Overlay
            _hotkeyManager.RegisterGlobalHotkey(
                HotkeyManager.HOTKEY_TOGGLE_OVERLAY,
                HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT,
                HotkeyManager.VK_O,
                () => Dispatcher.Invoke(() =>
                    OverlayModeButton_Click(OverlayModeButton, new RoutedEventArgs())));

            // Ctrl+Shift+C — Copy last translation
            _hotkeyManager.RegisterGlobalHotkey(
                HotkeyManager.HOTKEY_COPY_TRANSLATION,
                HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT,
                HotkeyManager.VK_C,
                () => Dispatcher.Invoke(() =>
                {
                    try
                    {
                        string text = Translator.Caption?.DisplayTranslatedCaption ?? "";
                        if (!string.IsNullOrEmpty(text))
                        {
                            System.Windows.Clipboard.SetText(text);
                            SnackbarHost.Show("Copied.", text, SnackbarType.Info, 100);
                        }
                    }
                    catch { }
                }));
        }

        private void TogglePause()
        {
            LogOnlyButton_Click(LogOnlyButton, new RoutedEventArgs());
        }

        #endregion

        #region Compact Mode

        private void CompactModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (!IsCompactMode)
            {
                // Save current dimensions
                _normalWidth = this.Width;
                _normalHeight = this.Height;
                _normalMinWidth = this.MinWidth;
                _normalMinHeight = this.MinHeight;

                // Enter compact mode
                IsCompactMode = true;
                symbolIcon.Symbol = SymbolRegular.ArrowMaximize20;
                symbolIcon.Filled = true;

                // Hide navigation and shrink
                RootNavigation.IsPaneToggleVisible = false;
                RootNavigation.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                RootNavigation.OpenPaneLength = 0;
                RootNavigation.Navigate(typeof(CaptionPage));

                this.MinWidth = 350;
                this.MinHeight = 90;
                this.Width = 500;
                this.Height = 100;

                // Hide non-essential buttons
                CaptionLogButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Exit compact mode
                IsCompactMode = false;
                symbolIcon.Symbol = SymbolRegular.ArrowMinimize20;
                symbolIcon.Filled = false;

                // Restore navigation
                RootNavigation.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                RootNavigation.OpenPaneLength = 48;

                this.MinWidth = _normalMinWidth > 0 ? _normalMinWidth : 750;
                this.MinHeight = _normalMinHeight > 0 ? _normalMinHeight : 170;
                this.Width = _normalWidth > 0 ? _normalWidth : 750;
                this.Height = _normalHeight > 0 ? _normalHeight : 170;
            }
        }

        #endregion

        #region Existing Event Handlers

        private void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopmost(!this.Topmost);
        }

        private void OverlayModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (OverlayWindow == null)
            {
                symbolIcon.Symbol = SymbolRegular.ClosedCaption24;
                symbolIcon.Filled = true;

                OverlayWindow = new OverlayWindow();
                OverlayWindow.SizeChanged +=
                    (s, e) => WindowHandler.SaveState(OverlayWindow, Translator.Setting);
                OverlayWindow.LocationChanged +=
                    (s, e) => WindowHandler.SaveState(OverlayWindow, Translator.Setting);

                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                var windowState = WindowHandler.LoadState(OverlayWindow, Translator.Setting);
                if (windowState.Left <= 0 || windowState.Left >= screenWidth ||
                    windowState.Top <= 0 || windowState.Top >= screenHeight)
                {
                    WindowHandler.RestoreState(OverlayWindow, new Rect(
                        (screenWidth - 650) / 2, screenHeight * 5 / 6 - 135, 650, 135));
                }
                else
                    WindowHandler.RestoreState(OverlayWindow, windowState);

                OverlayWindow.Show();
            }
            else
            {
                symbolIcon.Symbol = SymbolRegular.ClosedCaptionOff24;
                symbolIcon.Filled = false;

                switch (OverlayWindow.OnlyMode)
                {
                    case CaptionVisible.TranslationOnly:
                        OverlayWindow.OnlyMode = CaptionVisible.SubtitleOnly;
                        OverlayWindow.OnlyMode = CaptionVisible.Both;
                        break;
                    case CaptionVisible.SubtitleOnly:
                        OverlayWindow.OnlyMode = CaptionVisible.Both;
                        break;
                }

                OverlayWindow.Close();
                OverlayWindow = null;
            }
        }

        private void LogOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (Translator.LogOnlyFlag)
            {
                Translator.LogOnlyFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                Translator.LogOnlyFlag = true;
                symbolIcon.Filled = true;
            }

            Translator.ClearContexts();
        }

        private void CaptionLogButton_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.MainWindow.CaptionLogEnabled = !Translator.Setting.MainWindow.CaptionLogEnabled;
            ShowLogCard(Translator.Setting.MainWindow.CaptionLogEnabled);
            CaptionPage.Instance?.AutoHeight();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            WindowHandler.SaveState(window, Translator.Setting);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainWindow_LocationChanged(sender, e);
            IsAutoHeight = false;
        }

        public void ToggleTopmost(bool enabled)
        {
            var button = TopmostButton as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            symbolIcon.Filled = enabled;
            this.Topmost = enabled;
            Translator.Setting.MainWindow.Topmost = enabled;
        }

        #endregion

        #region First Use & Updates

        private void CheckForFirstUse()
        {
            if (!Translator.FirstUseFlag)
                return;

            RootNavigation.Navigate(typeof(SettingPage));
            LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);

            Dispatcher.InvokeAsync(() =>
            {
                var welcomeWindow = new WelcomeWindow
                {
                    Owner = this
                };
                welcomeWindow.Show();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private async Task CheckForUpdates()
        {
            if (Translator.FirstUseFlag)
                return;

            string latestVersion = string.Empty;
            try
            {
                latestVersion = await UpdateUtil.GetLatestVersion();
            }
            catch (Exception ex)
            {
                SnackbarHost.Show("[ERROR] Update Check Failed.", ex.Message, SnackbarType.Error,
                    timeout: 2, closeButton: true);

                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var ignoredVersion = Translator.Setting.IgnoredUpdateVersion;
            if (!string.IsNullOrEmpty(ignoredVersion) && ignoredVersion == latestVersion)
                return;
            if (!string.IsNullOrEmpty(latestVersion) && latestVersion != currentVersion)
            {
                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "New Version Available",
                    Content = $"A new version has been detected: {latestVersion}\n" +
                              $"Current version: {currentVersion}\n" +
                              $"Please visit GitHub to download the latest release.",
                    PrimaryButtonText = "Update",
                    CloseButtonText = "Ignore this version"
                };
                var result = await dialog.ShowDialogAsync();

                if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    var url = UpdateUtil.GitHubReleasesUrl;
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        SnackbarHost.Show("[ERROR] Open Browser Failed.", ex.Message, SnackbarType.Error,
                            timeout: 2, closeButton: true);
                    }
                }
                else
                    Translator.Setting.IgnoredUpdateVersion = latestVersion;
            }
        }

        #endregion

        #region UI Helpers

        public void ShowLogCard(bool enabled)
        {
            if (CaptionLogButton.Icon is SymbolIcon icon)
            {
                if (enabled)
                    icon.Symbol = SymbolRegular.History24;
                else
                    icon.Symbol = SymbolRegular.HistoryDismiss24;
                CaptionPage.Instance?.CollapseTranslatedCaption(enabled);
            }
        }

        public void AutoHeightAdjust(int minHeight = -1, int maxHeight = -1)
        {
            if (IsCompactMode) return; // Don't auto-resize in compact mode

            if (minHeight > 0 && Height < minHeight)
            {
                Height = minHeight;
                IsAutoHeight = true;
            }

            if (IsAutoHeight && maxHeight > 0 && Height > maxHeight)
                Height = maxHeight;
        }

        #endregion
    }
}
