using Microsoft.UI.Xaml.Controls.Primitives;

namespace AccountSettings;

public sealed partial class MainPage : Page
{
    public bool IsAdvancedMode
    {
        get { return (bool)GetValue(IsAdvancedModeProperty); }
        set { SetValue(IsAdvancedModeProperty, value); }
    }

    public static readonly DependencyProperty IsAdvancedModeProperty =
        DependencyProperty.Register(nameof(IsAdvancedMode), typeof(bool), typeof(MainPage), new PropertyMetadata(false, OnAdvancedModeChanged));

    private static void OnAdvancedModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MainPage mainPage && e.NewValue is bool isAdvancedMode)
        {
            if (isAdvancedMode)
            {
                mainPage.RotateChevronTo180();
            }
            else
            {
                mainPage.RotateChevronTo0();
            }
        }
    }

    public MainPage()
    {
        this.InitializeComponent();

        // The first version of the 'Advanced settings' switch
        // SettingsModeSelector.SelectedIndex = 0;
    }

    private void OnSettingModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combobox)
        {
            IsAdvancedMode = combobox.SelectedIndex == 1;
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

    public void RotateChevronTo180()
    {
        RotateChevronTo180Degrees.Begin();
    }

    public void RotateChevronTo0()
    {
        RotateChevronTo0Degrees.Begin();
    }

    private void OnSecondPasswordInfoClick(object sender, RoutedEventArgs e)
    {
        FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
    }
}
