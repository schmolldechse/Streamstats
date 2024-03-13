using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Streamstats.src.UI
{
    public class BorderBrushConverter : IMultiValueConverter
    {

        private SolidColorBrush GRAY = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333742"));
        private SolidColorBrush PURPLE = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D28D9"));
        private SolidColorBrush GREEN = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); //388E3C

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || !(values[0] is bool) || !(values[1] is bool)) return null;

            bool isMouseOver = (bool)values[0];
            bool isPressed = (bool)values[1];

            if (isMouseOver && !isPressed)
            {
                return this.PURPLE;
            } else if (isMouseOver && isPressed)
            {
                return this.GREEN;
            } else
            {
                return this.GRAY;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
