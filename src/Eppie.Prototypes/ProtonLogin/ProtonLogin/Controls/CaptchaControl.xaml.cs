// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProtonLogin.Controls;
public sealed partial class CaptchaControl : UserControl
{
    public class HumanVerificationEventArgs : EventArgs
    {
        public bool IsSuccess { get; }

        public string Type { get; }
        public string Token { get; }

        public HumanVerificationEventArgs(bool isSuccess, string type, string token)
        {
            IsSuccess = isSuccess;
            Type = type;
            Token = token;
        }
    }

    public class CaptchaResponseJson
    {
        public static readonly string CaptchaTypeKey = "pm_captcha";

        [JsonInclude]
        [JsonPropertyName("type")]
        public string? Type { get; private set; }

        [JsonInclude]
        [JsonPropertyName("token")]
        public string? Token { get; private set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Type) && Type == CaptchaTypeKey;
        }
    }

    public event EventHandler<HumanVerificationEventArgs>? HumanVerificationCompleted;

    public Uri CaptchaUri
    {
        get { return (Uri)GetValue(CaptchaUriProperty); }
        set { SetValue(CaptchaUriProperty, value); }
    }

    public static readonly DependencyProperty CaptchaUriProperty =
        DependencyProperty.Register(nameof(CaptchaUri), typeof(Uri), typeof(CaptchaControl), new PropertyMetadata(null));

    public CaptchaControl()
    {
        this.InitializeComponent();
    }

    private void OnWebMessageReceived(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.WebMessageAsJson))
        {
            return;
        }

        CaptchaResponseJson? json = JsonSerializer.Deserialize<CaptchaResponseJson>(args.WebMessageAsJson);

        if (json?.IsValid() is true)
        {
            HumanVerificationCompleted?.Invoke(this, new HumanVerificationEventArgs(true, "captcha", json.Token ?? string.Empty));
        }
    }
}
