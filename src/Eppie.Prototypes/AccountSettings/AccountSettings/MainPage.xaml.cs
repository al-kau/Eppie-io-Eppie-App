namespace AccountSettings;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        SettingsModeSelector.SelectedIndex = 0;
    }

    private void OnSettingModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combobox)
        {
            CustomSection.Visibility = combobox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private async void OnApplyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyText.Opacity = 0;
            ApplyProgress.Visibility = Visibility.Visible;

            await Task.Delay(2000).ConfigureAwait(true);

            ApplyProgress.Visibility = Visibility.Collapsed;
            ApplyText.Opacity = 1;
        }
        catch { }
    }
}
