namespace Settings;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LanguageCombobox.SelectedIndex = 0;
        ThemeCombobox.SelectedIndex = 0;
        LogCombobox.SelectedIndex = 0;
    }
}
