using System.Globalization;
using System.Windows.Data;

namespace TinyPlayer.Desktop.Converters
{
    internal class BoolToPlayPauseConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => (bool)value ? "⏸" : "▶";

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
