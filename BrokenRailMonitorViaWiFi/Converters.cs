﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BrokenRailMonitorViaWiFi
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
            int stress = (int)value;
            return stress == 0 ? String.Empty : stress.ToString();
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
}
