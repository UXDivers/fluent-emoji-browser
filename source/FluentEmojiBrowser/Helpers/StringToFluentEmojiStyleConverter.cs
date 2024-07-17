using System.Globalization;
namespace FluentEmojiBrowser
{
    public class StringToFluentEmojiStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString();

            if (s == null)
            {
                return FluentEmojiStyle.Color;
            }
            
            return s switch
            {
                "3D" => FluentEmojiStyle.ThreeD,
                "Color" => FluentEmojiStyle.Color,
                "Flat" => FluentEmojiStyle.Flat,
                "High Contrast" => FluentEmojiStyle.HighContrast
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
