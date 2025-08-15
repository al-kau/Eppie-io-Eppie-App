// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AccountSettings;
public sealed partial class OptionControl : UserControl
{
    public UIElement Header
    {
        get { return (UIElement)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register("Header", typeof(UIElement), typeof(OptionControl), new PropertyMetadata(null));

    public UIElement OptionContent
    {
        get { return (UIElement)GetValue(OptionContentProperty); }
        set { SetValue(OptionContentProperty, value); }
    }

    public static readonly DependencyProperty OptionContentProperty =
        DependencyProperty.Register("OptionContent", typeof(UIElement), typeof(OptionControl), new PropertyMetadata(null));


    public OptionControl()
    {
        this.InitializeComponent();
    }
}
