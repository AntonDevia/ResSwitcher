using System.Windows;
using System.Windows.Media;
using ResSwitcher.Core;
using Wpf.Ui.Controls;

namespace ResSwitcher.Settings;

public partial class MainWindow : FluentWindow
{
    private AppConfig _config = new();
    private string _selectedRatio = "4:3";
    private bool _isLoading = true;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        _isLoading = true;
        _config = AppConfig.Load();
        ConfigPathText.Text = AppConfig.ConfigPath;

        _selectedRatio = AspectRatios.Presets.ContainsKey(_config.AspectRatio) ? _config.AspectRatio : "4:3";
        HighlightSelectedTile(_selectedRatio);
        PopulateResolutionCombo(_selectedRatio);

        var matchingPreset = AspectRatios.GetAvailablePresets(_selectedRatio)
            .FirstOrDefault(p => p.Width == _config.TargetWidth && p.Height == _config.TargetHeight);

        if (matchingPreset is not null)
        {
            ResolutionCombo.SelectedItem = matchingPreset;
            CustomResCheck.IsChecked = false;
        }
        else
        {
            CustomResCheck.IsChecked = true;
            CustomWidthBox.Value = _config.TargetWidth;
            CustomHeightBox.Value = _config.TargetHeight;
        }

        RevertOnFocusToggle.IsChecked = _config.RevertOnFocusLoss;
        StartWithWindowsToggle.IsChecked = AutoStartManager.IsEnabled();
        DoubleF11Toggle.IsChecked = _config.DoubleF11Fix;
        DoubleF11DelayBox.Value = _config.DoubleF11DelayMs;
        DoubleF11DelayPanel.Visibility = _config.DoubleF11Fix ? Visibility.Visible : Visibility.Collapsed;
        KeyModeToggle.IsChecked = _config.DoubleF11Mode == KeySendMode.DirectToWindow;
        UpdateKeyModeLabel();

        if (_config.RefreshRate > 0)
        {
            CustomRefreshRateCheck.IsChecked = true;
            RefreshRateBox.Value = _config.RefreshRate;
        }
        else
        {
            CustomRefreshRateCheck.IsChecked = false;
            RefreshRateBox.Value = null;
        }

        _isLoading = false;
        ValidateAndUpdateWarning();
    }

    private void HighlightSelectedTile(string ratio)
    {
        var brush = (Brush)FindResource("AccentFillColorDefaultBrush");
        var normal = (Brush)FindResource("ControlStrokeColorDefaultBrush");

        Tile4x3.BorderBrush = ratio == "4:3" ? brush : normal;
        Tile16x9.BorderBrush = ratio == "16:9" ? brush : normal;
        Tile16x10.BorderBrush = ratio == "16:10" ? brush : normal;

        Tile4x3.BorderThickness = new Thickness(ratio == "4:3" ? 2 : 1);
        Tile16x9.BorderThickness = new Thickness(ratio == "16:9" ? 2 : 1);
        Tile16x10.BorderThickness = new Thickness(ratio == "16:10" ? 2 : 1);
    }

    private void PopulateResolutionCombo(string ratio)
    {
        ResolutionCombo.ItemsSource = AspectRatios.GetAvailablePresets(ratio);
        ResolutionCombo.DisplayMemberPath = nameof(ResolutionPreset.Label);
        if (ResolutionCombo.Items.Count > 0)
            ResolutionCombo.SelectedIndex = 0;
    }

    private void RatioTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string ratio) return;

        _selectedRatio = ratio;
        HighlightSelectedTile(ratio);
        PopulateResolutionCombo(ratio);
        ValidateAndUpdateWarning();
    }

    private void ResolutionCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ValidateAndUpdateWarning();
    }

    private void CustomResCheck_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        ResolutionCombo.IsEnabled = CustomResCheck.IsChecked != true;
        ValidateAndUpdateWarning();
    }

    private void CustomResBox_ValueChanged(object sender, RoutedEventArgs e)
    {
        ValidateAndUpdateWarning();
    }

    private void CustomRefreshRateCheck_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        if (CustomRefreshRateCheck.IsChecked != true)
            RefreshRateBox.Value = null;
    }

    private void DoubleF11Toggle_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        DoubleF11DelayPanel.Visibility = DoubleF11Toggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void KeyModeToggle_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        UpdateKeyModeLabel();
    }

    private void UpdateKeyModeLabel()
    {
        KeyModeLabel.Text = KeyModeToggle.IsChecked == true
            ? "Напрямую в окно игры"
            : "Глобально (как физическая клавиша)";
    }

    private void ValidateAndUpdateWarning()
    {
        if (_isLoading) return;

        var (width, height) = GetSelectedResolution();
        if (width <= 0 || height <= 0)
        {
            RatioWarningBar.IsOpen = false;
            return;
        }

        bool matches = AspectRatios.MatchesRatio(width, height, _selectedRatio, tolerance: 0.02);
        RatioWarningBar.IsOpen = CustomResCheck.IsChecked == true && !matches;
    }

    private (int Width, int Height) GetSelectedResolution()
    {
        if (CustomResCheck.IsChecked == true)
        {
            int w = (int)(CustomWidthBox.Value ?? 0);
            int h = (int)(CustomHeightBox.Value ?? 0);
            return (w, h);
        }

        if (ResolutionCombo.SelectedItem is ResolutionPreset preset)
            return (preset.Width, preset.Height);

        return (0, 0);
    }

    private void CopyAuthorButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(AuthorNickText.Text);
            StatusText.Text = "Ник автора скопирован.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Не удалось скопировать: {ex.Message}";
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var (width, height) = GetSelectedResolution();

        if (width < 320 || height < 240)
        {
            StatusText.Text = "Укажите корректное разрешение (минимум 320x240).";
            return;
        }

        int refreshRate = 0;
        if (CustomRefreshRateCheck.IsChecked == true)
        {
            refreshRate = (int)(RefreshRateBox.Value ?? 0);
            if (refreshRate < 24)
            {
                StatusText.Text = "Укажите корректную частоту обновления (минимум 24 Гц) или выключи 'Свою частоту'.";
                return;
            }
        }

        _config.AspectRatio = _selectedRatio;
        _config.TargetWidth = width;
        _config.TargetHeight = height;
        _config.RefreshRate = refreshRate;
        _config.RevertOnFocusLoss = RevertOnFocusToggle.IsChecked == true;
        _config.DoubleF11Fix = DoubleF11Toggle.IsChecked == true;
        _config.DoubleF11DelayMs = (int)(DoubleF11DelayBox.Value ?? 1500);
        _config.DoubleF11Mode = KeyModeToggle.IsChecked == true ? KeySendMode.DirectToWindow : KeySendMode.Global;

        try
        {
            _config.Save();

            bool wantAutoStart = StartWithWindowsToggle.IsChecked == true;
            if (wantAutoStart && !AutoStartManager.IsEnabled())
            {
                var exePath = AutoStartManager.ResolveServiceExePath();
                if (exePath is not null)
                    AutoStartManager.Enable(exePath);
                else
                    StatusText.Text = "Сохранено, но ResSwitcher.exe не найден рядом — автозапуск не включён.";
            }
            else if (!wantAutoStart && AutoStartManager.IsEnabled())
            {
                AutoStartManager.Disable();
            }

            StatusText.Text = $"Сохранено — {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка сохранения: {ex.Message}";
        }
    }
}
