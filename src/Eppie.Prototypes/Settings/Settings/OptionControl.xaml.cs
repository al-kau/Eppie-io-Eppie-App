using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;

namespace Settings;
public sealed partial class OptionControl : UserControl
{
    public string Caption
    {
        get { return (string)GetValue(CaptionProperty); }
        set { SetValue(CaptionProperty, value); }
    }

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(OptionControl), new PropertyMetadata(null));


    public Inline InlineNote
    {
        get { return (Inline)GetValue(InlineNoteProperty); }
        set { SetValue(InlineNoteProperty, value); }
    }

    public static readonly DependencyProperty InlineNoteProperty =
        DependencyProperty.Register(nameof(InlineNote), typeof(Inline), typeof(OptionControl), new PropertyMetadata(null, OnInlineNotePropertyChanged));

    private static void OnInlineNotePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OptionControl control && e.NewValue is Inline inline)
        {
            control.NoteBlock.Inlines.Clear();
            control.NoteBlock.Inlines.Add(inline);
        }
    }


    public string TextNote
    {
        get { return (string)GetValue(TextNoteProperty); }
        set { SetValue(TextNoteProperty, value); }
    }

    public static readonly DependencyProperty TextNoteProperty =
        DependencyProperty.Register(nameof(TextNote), typeof(string), typeof(OptionControl), new PropertyMetadata(null, OnTextNotePropertyChanged));

    private static void OnTextNotePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OptionControl control && e.NewValue is string text && control.InlineNote is null)
        {
            control.NoteBlock.Text = text;
        }
    }


    public double IndentHeight
    {
        get { return (double)GetValue(IndentHeightProperty); }
        set { SetValue(IndentHeightProperty, value); }
    }

    public static readonly DependencyProperty IndentHeightProperty =
        DependencyProperty.Register(nameof(IndentHeight), typeof(double), typeof(OptionControl), new PropertyMetadata(36.0));


    public UIElement OptionContent
    {
        get { return (UIElement)GetValue(OptionContentProperty); }
        set { SetValue(OptionContentProperty, value); }
    }

    public static readonly DependencyProperty OptionContentProperty =
        DependencyProperty.Register(nameof(OptionContent), typeof(UIElement), typeof(OptionControl), new PropertyMetadata(null));


    public double ActualIndentHeight
    {
        get { return (double)GetValue(ActualIndentHeightProperty); }
        set { SetValue(ActualIndentHeightProperty, value); }
    }

    public static readonly DependencyProperty ActualIndentHeightProperty =
        DependencyProperty.Register("ActualIndentHeight", typeof(double), typeof(OptionControl), new PropertyMetadata(0.0));


    public double TextHeight
    {
        get { return (double)GetValue(TextHeightProperty); }
        set { SetValue(TextHeightProperty, value); }
    }

    public static readonly DependencyProperty TextHeightProperty =
        DependencyProperty.Register(nameof(TextHeight), typeof(double), typeof(OptionControl), new PropertyMetadata(0.0));



    public bool HideNoteMode
    {
        get { return (bool)GetValue(HideNoteModeProperty); }
        set { SetValue(HideNoteModeProperty, value); }
    }

    public static readonly DependencyProperty HideNoteModeProperty =
        DependencyProperty.Register(nameof(HideNoteMode), typeof(bool), typeof(OptionControl), new PropertyMetadata(false));



    public OptionControl()
    {
        this.InitializeComponent();
    }
}
