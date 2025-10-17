// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ProtonLogin.Controls;
public sealed partial class TwoFactorAuthenticationControl : UserControl
{

    public class TwoFactorAuthenticationEventArgs : EventArgs
    {
        public string Code { get; }

        public TwoFactorAuthenticationEventArgs(string code)
        {
            Code = code;
        }
    }

    public event EventHandler<TwoFactorAuthenticationEventArgs>? AuthenticateButtonClick;

    public TwoFactorAuthenticationControl()
    {
        this.InitializeComponent();
    }

    private void OnAuthenticateButtonClick(object sender, RoutedEventArgs e)
    {
        AuthenticateButtonClick?.Invoke(this, new TwoFactorAuthenticationEventArgs(TwoFactorCodeTextBox.Text));
    }
}
