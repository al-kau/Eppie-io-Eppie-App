using Microsoft.UI;

namespace ProtonLogin;

public sealed partial class MainPage : Page
{
    private Brush OutputBrush { get; }

    public MainPage()
    {
        this.InitializeComponent();
        OutputBrush = OutputBox.Foreground;
    }

    private void OnErrorOccurred(object sender, Controls.ProtonSiginControl.ErrorEventArgs e)
    {
        OutputBox.Foreground = new SolidColorBrush(Colors.Red);
        OutputBox.Text = e.Exception.Message;
    }

    private void OnOutputChanged(object sender, Controls.ProtonSiginControl.OutputEventArgs e)
    {
        OutputBox.Foreground = OutputBrush;
        OutputBox.Text = e.Output;
    }
}
