// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ProtonLogin.Controls;
public sealed partial class AuthenticationControl : UserControl
{
    public class SiginEventArgs : EventArgs
    {
        public string Email { get; }
        public string Password { get; }

        public SiginEventArgs(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    public event EventHandler<SiginEventArgs>? SignInClick;

    public AuthenticationControl()
    {
        this.InitializeComponent();
    }

    private void OnSignInClick(object sender, RoutedEventArgs e)
    {
        SignInClick?.Invoke(this, new SiginEventArgs(EmailTextBox.Text, UserPasswordBox.Password));
    }
}
