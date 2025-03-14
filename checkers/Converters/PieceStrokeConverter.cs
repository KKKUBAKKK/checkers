using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace checkers.Converters
{
    public class PieceStrokeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string pieceColor)
            {
                return pieceColor == "Black" ? "White" : "Black";
            }
            return "Black";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strokeColor)
            {
                return strokeColor == "Black" ? "White" : "Black";
            }
            return "Black";
        }
    }
}