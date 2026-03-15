using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using LIveSubs.utils;

namespace LIveSubs
{
    public partial class CaptionPage : Page
    {
        public const int CARD_HEIGHT = 110;

        private static CaptionPage instance;
        public static CaptionPage Instance => instance;

        // Status indicator brushes
        private static readonly SolidColorBrush GreenBrush = new(System.Windows.Media.Color.FromRgb(76, 175, 80));   // Connected
        private static readonly SolidColorBrush YellowBrush = new(System.Windows.Media.Color.FromRgb(255, 193, 7));  // Translating
        private static readonly SolidColorBrush RedBrush = new(System.Windows.Media.Color.FromRgb(244, 67, 54));     // Error
        private static readonly SolidColorBrush GrayBrush = new(System.Windows.Media.Color.FromRgb(158, 158, 158));  // Paused

        public CaptionPage()
        {
            InitializeComponent();
            DataContext = Translator.Caption;
            instance = this;

            Loaded += (s, e) =>
            {
                AutoHeight();
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Visible;
                Translator.Caption.PropertyChanged += TranslatedChanged;
                UpdateStatusBar();
            };
            Unloaded += (s, e) =>
            {
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Collapsed;
                Translator.Caption.PropertyChanged -= TranslatedChanged;
            };

            CollapseTranslatedCaption(Translator.Setting.MainWindow.CaptionLogEnabled);
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    System.Windows.Clipboard.SetText(textBlock.Text);
                    SnackbarHost.Show("Copied.", textBlock.Text, SnackbarType.Info, 100);
                }
                catch
                {
                    SnackbarHost.Show("Copy Failed.", string.Empty, SnackbarType.Error, 100);
                }
                await Task.Delay(500);
            }
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Translator.Caption.DisplayTranslatedCaption))
            {
                if (Encoding.UTF8.GetByteCount(Translator.Caption.DisplayTranslatedCaption) >= TextUtil.LONG_THRESHOLD)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 15;
                    }), DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 18;
                    }), DispatcherPriority.Background);
                }

                // Update status bar when translation changes
                Dispatcher.BeginInvoke(new Action(UpdateStatusBar), DispatcherPriority.Background);
            }
        }

        private void UpdateStatusBar()
        {
            try
            {
                // Show API name
                StatusApiName.Text = Translator.Setting?.ApiName ?? "Unknown";

                // Determine status color
                string translatedText = Translator.Caption?.DisplayTranslatedCaption ?? "";

                if (Translator.LogOnlyFlag)
                {
                    StatusIndicator.Fill = GrayBrush;
                    StatusLatency.Text = "⏸ Paused";
                }
                else if (translatedText.Contains("[ERROR]"))
                {
                    StatusIndicator.Fill = RedBrush;
                    StatusLatency.Text = "⚠ Error";
                }
                else if (translatedText.Contains("[WARNING]"))
                {
                    StatusIndicator.Fill = YellowBrush;
                    StatusLatency.Text = "⚠ Warning";
                }
                else if (string.IsNullOrEmpty(translatedText))
                {
                    StatusIndicator.Fill = YellowBrush;
                    StatusLatency.Text = "Waiting...";
                }
                else
                {
                    StatusIndicator.Fill = GreenBrush;
                    StatusLatency.Text = "● Active";
                }
            }
            catch
            {
                // Ignore status bar update errors
            }
        }

        public void CollapseTranslatedCaption(bool isCollapsed)
        {
            var converter = new GridLengthConverter();

            if (isCollapsed)
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("Auto");
                LogCards.Visibility = Visibility.Visible;
                StatusBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("*");
                LogCards.Visibility = Visibility.Collapsed;
                StatusBar.Visibility = Visibility.Visible;
            }
        }

        public void AutoHeight()
        {
            if (Translator.Setting.MainWindow.CaptionLogEnabled)
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: CARD_HEIGHT * (Translator.Setting.DisplaySentences + 1),
                    maxHeight: CARD_HEIGHT * (Translator.Setting.DisplaySentences + 1));
            else
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: (int)App.Current.MainWindow.MinHeight,
                    maxHeight: (int)App.Current.MainWindow.MinHeight);
        }
    }
}
