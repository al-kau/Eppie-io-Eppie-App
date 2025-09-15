using Microsoft.UI.Xaml.Data;

namespace Settings.Converters;

public sealed partial class DoubleToGridLengthConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double indentHeight)
        {
            return new GridLength(indentHeight);
        }
        return GridLength.Auto;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}


public abstract partial class BoolToValueConverter<TValue> : IValueConverter
    where TValue : notnull
{
    public TValue TrueValue { get; set; } = default!;
    public TValue FalseValue { get; set; } = default!;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueValue : FalseValue;
        }
        return FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}


public sealed partial class BoolToVisabilityConverter : BoolToValueConverter<Visibility>
{
    public BoolToVisabilityConverter()
        : base()
    {
        TrueValue = Visibility.Visible;
        FalseValue = Visibility.Collapsed;
    }
}

public sealed partial class BoolToIntConverter : BoolToValueConverter<Visibility> { };
public sealed partial class BoolToVerticalAlignmentConverter : BoolToValueConverter<VerticalAlignment> { };


