using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SurfacePlot
{
    public class MyBrushHelper
    {
        /// <summary>
        /// Creates a rainbow brush.
        /// </summary>
        /// <returns>
        /// A rainbow brush.
        /// </returns>
        public static LinearGradientBrush CreateRainbowBrush(bool horizontal = true)
        {
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = horizontal ? new Point(1, 0) : new Point(0, 1) };
            brush.GradientStops.Add(new GradientStop(Colors.Red, 1.00));
            brush.GradientStops.Add(new GradientStop(Colors.Orange, 0.84));
            brush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.67));
            brush.GradientStops.Add(new GradientStop(Colors.Green, 0.50));
            brush.GradientStops.Add(new GradientStop(Colors.Blue, 0.33));
            brush.GradientStops.Add(new GradientStop(Colors.Indigo, 0.17));
            brush.GradientStops.Add(new GradientStop(Colors.Violet, 0.00));
            return brush;
        }
    }
}
