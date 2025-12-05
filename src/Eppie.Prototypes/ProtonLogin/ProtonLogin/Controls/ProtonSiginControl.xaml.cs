// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

using System;
using System.Diagnostics.Tracing;
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

    private Task<(bool,string,string)> AskProtonCpatcha(Uri uri, Exception ex, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<(bool,string,string)>();

        CaptchaControl.HumanVerificationCompleted += OnCompleted;
        CaptchaControl.HumanVerificationCancelled += OnCancelled;
        SetCaptchaUri(uri);

        if (ex != null)
        {
            OnError(ex);
        }

        GotoPhase(SignInPhase.Captcha);

        return tcs.Task;

        void OnCompleted(object? sender, CaptchaControl.HumanVerificationEventArgs eventArgs)
        {
            CaptchaControl.HumanVerificationCompleted -= OnCompleted;
            CaptchaControl.HumanVerificationCancelled -= OnCancelled;

            OutputChanged?.Invoke(this, new OutputEventArgs($$"""
                Human Verification: Completed
                Type: {{eventArgs.Type}}
                Token: {{eventArgs.Token}}
                """));

            tcs.SetResult((true, eventArgs.Type, eventArgs.Token));
        }

        void OnCancelled(object? sender, EventArgs eventArgs)
        {
            CaptchaControl.HumanVerificationCompleted -= OnCompleted;
            CaptchaControl.HumanVerificationCancelled -= OnCancelled;

            OutputChanged?.Invoke(this, new OutputEventArgs("Human Verification: Cancelled"));

            tcs.SetResult((false, string.Empty, string.Empty));
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

    private Task<(bool, string)> AskTwoFactorCode(Exception ex, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<(bool, string)>();

        TwoFactorAuthenticationControl.AuthenticateButtonClick += OnEvent;
        TwoFactorAuthenticationControl.CancelButtonClick += OnCancel;

        if (ex != null)
        {
            OnError(ex);
        }

        GotoPhase(SignInPhase.TwoFactor);

        return tcs.Task;

        void OnEvent(object? sender, TwoFactorAuthenticationControl.TwoFactorAuthenticationEventArgs eventArgs)
        {
            TwoFactorAuthenticationControl.AuthenticateButtonClick -= OnEvent;
            TwoFactorAuthenticationControl.CancelButtonClick -= OnCancel;

            tcs.SetResult((true, eventArgs.Code));
        }

        void OnCancel(object? sender, EventArgs eventArgs)
        {
            TwoFactorAuthenticationControl.AuthenticateButtonClick -= OnEvent;
            TwoFactorAuthenticationControl.CancelButtonClick -= OnCancel;

            tcs.SetResult((false, string.Empty));
        }
    }

    private Task<(bool, string)> AskMailboxPassword(Exception ex, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<(bool, string)>();

        MailboxPasswordControl.UnlockClick += OnEvent;
        MailboxPasswordControl.CancelButtonClick += OnCancel;

        if(ex != null)
        {
            OnError(ex);
        }

        GotoPhase(SignInPhase.MailboxPassord);

        return tcs.Task;

        void OnEvent(object? sender, MailboxPasswordControl.UnlockMailboxEventArgs eventArgs)
        {
            MailboxPasswordControl.UnlockClick -= OnEvent;
            MailboxPasswordControl.CancelButtonClick -= OnCancel;

            tcs.SetResult((true, eventArgs.MailboxPassword));
        }

        void OnCancel(object? sender, EventArgs eventArgs)
        {
            MailboxPasswordControl.UnlockClick -= OnEvent;
            MailboxPasswordControl.CancelButtonClick -= OnCancel;

            tcs.SetResult((false, string.Empty));
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

    private async void OnError(Exception ex)
    {
        try
        {
            await RunAsync(() =>
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
            }).ConfigureAwait(true);
        }
        catch
        {
        }
    }
}
