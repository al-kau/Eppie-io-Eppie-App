// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

using Microsoft.UI.Dispatching;

namespace ProtonLogin.Controls;
public sealed partial class ProtonSiginControl : UserControl
{
    enum SignInPhase
    {
        Failed,
        Authentication,
        Captcha,
        TwoFactor,
        MailboxPassord,
        Done
    }

    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public ErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }

    public class OutputEventArgs : EventArgs
    {
        public string Output { get; }

        public OutputEventArgs(string output = "")
        {
            Output = output;
        }
    }

    public event EventHandler<ErrorEventArgs>? ErrorOccurred;
    public event EventHandler<OutputEventArgs>? OutputChanged;

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public ProtonSiginControl()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        GotoPhase(SignInPhase.Authentication);
    }

    private async void GotoPhase(SignInPhase state)
    {
        try
        {
            await RunAsync(() =>
            {
                AuthenticationControl.Visibility = state == SignInPhase.Authentication ? Visibility.Visible : Visibility.Collapsed;
                CaptchaControl.Visibility = state == SignInPhase.Captcha ? Visibility.Visible : Visibility.Collapsed;
                TwoFactorAuthenticationControl.Visibility = state == SignInPhase.TwoFactor ? Visibility.Visible : Visibility.Collapsed;
                MailboxPasswordControl.Visibility = state == SignInPhase.MailboxPassord ? Visibility.Visible : Visibility.Collapsed;
                FailedPanel.Visibility = state == SignInPhase.Failed ? Visibility.Visible : Visibility.Collapsed;
                DonePanel.Visibility = state == SignInPhase.Done ? Visibility.Visible : Visibility.Collapsed;
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
        }
    }

    private async void OnSignInClick(object sender, AuthenticationControl.SiginEventArgs e)
    {
        try
        {
            string email = e.Email;
            string password = e.Password;

            (string userId, string refreshToken, string saltedKeyPass) = await Tuvi.Proton.ClientAuth.LoginFullAsync(
                    email,
                    password,
                    AskTwoFactorCode,
                    AskMailboxPassword,
                    AskProtonCpatcha,
                    default).ConfigureAwait(true);

            OutputChanged?.Invoke(this, new OutputEventArgs($"""
                Login Successful
                RefreshToken: {refreshToken}
                """));

            GotoPhase(SignInPhase.Done);
        }
        catch (OperationCanceledException)
        {
            GotoPhase(SignInPhase.Failed);
        }
        catch (Exception ex)
        {
            GotoPhase(SignInPhase.Failed);
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
        }
    }

    private Task<(bool,string,string)> AskProtonCpatcha(Uri uri, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<(bool,string,string)>();

        CaptchaControl.HumanVerificationCompleted += OnEvent;
        SetCaptchaUri(uri);

        GotoPhase(SignInPhase.Captcha);

        return tcs.Task;

        void OnEvent(object? sender, CaptchaControl.HumanVerificationEventArgs eventArgs)
        {
            CaptchaControl.HumanVerificationCompleted -= OnEvent;


            OutputChanged?.Invoke(this, new OutputEventArgs($$"""
                Success: {{eventArgs.IsSuccess}}
                Type: {{eventArgs.Type}}
                Token: {{eventArgs.Token}}
                """));

            tcs.SetResult((false, eventArgs.Type, eventArgs.Token));
        }
    }


    private async void SetCaptchaUri(Uri uri)
    {
        try
        {
            await RunAsync(() =>
            {
                CaptchaControl.CaptchaUri = uri;
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
        }
    }

    private Task<string> AskTwoFactorCode(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>();

        TwoFactorAuthenticationControl.AuthenticateButtonClick += OnEvent;

        GotoPhase(SignInPhase.TwoFactor);

        return tcs.Task;

        void OnEvent(object? sender, TwoFactorAuthenticationControl.TwoFactorAuthenticationEventArgs eventArgs)
        {
            TwoFactorAuthenticationControl.AuthenticateButtonClick -= OnEvent;
            tcs.SetResult(eventArgs.Code);
        }
    }

    private Task<string> AskMailboxPassword(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>();

        MailboxPasswordControl.UnlockClick += OnEvent;

        GotoPhase(SignInPhase.MailboxPassord);

        return tcs.Task;

        void OnEvent(object? sender, MailboxPasswordControl.UnlockMailboxEventArgs eventArgs)
        {
            MailboxPasswordControl.UnlockClick -= OnEvent;
            tcs.SetResult(eventArgs.MailboxPassword);
        }
    }


    private async Task RunAsync(Action action)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            var completionSource = new TaskCompletionSource<bool>();
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    completionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            });
            await completionSource.Task;
        }
    }

    private void OnAgainClick(object sender, RoutedEventArgs e)
    {
        OutputChanged?.Invoke(this, new OutputEventArgs());
        GotoPhase(SignInPhase.Authentication);
    }
}
