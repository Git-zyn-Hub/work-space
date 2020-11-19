using BrokenRail3MonitorViaWiFi.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BrokenRail3MonitorViaWiFi
{
    public class WhichRail2MarginConverters : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RailNo whichRail = (RailNo)value;
            switch (whichRail)
            {
                case RailNo.Rail1:
                    return new Thickness(0, -20, 0, 20);
                case RailNo.Rail2:
                    return new Thickness(0, 20, 0, -20);
                default:
                    return new Thickness(0, 0, 0, 0);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Stress2TextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int temp = (int)value;
            double stress = (double)temp / 100;
            return stress == 0 ? String.Empty : (stress.ToString() + "MPa");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Temperature2TextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int temperature = (int)value;
            return temperature == 0 ? String.Empty : (temperature.ToString() + "℃");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CurrentTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int timeStamp = (int)value;
            return TimeStamp.GetDateTime(timeStamp).ToString("yyyy/MM/dd h:mm:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Second2TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int second = (int)value;
            return TimeStamp.parseTimeSeconds(second, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
