// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ProtonLogin.Controls;
public sealed partial class MailboxPasswordControl : UserControl
{
    public class UnlockMailboxEventArgs : EventArgs
    {
        public string MailboxPassword { get; }

        public UnlockMailboxEventArgs(string password)
        {
            MailboxPassword = password;
        }
    }

    public event EventHandler<UnlockMailboxEventArgs>? UnlockClick;
    public event EventHandler? CancelButtonClick;

    public MailboxPasswordControl()
    {
        this.InitializeComponent();
    }

    private void OnUnlockButtonClick(object sender, RoutedEventArgs e)
    {
        UnlockClick?.Invoke(this, new UnlockMailboxEventArgs(MailboxPasswordBox.Password));
    }

    private void OnCanncelClick(object sender, RoutedEventArgs e)
    {
        CancelButtonClick?.Invoke(this, EventArgs.Empty);
    }
}
