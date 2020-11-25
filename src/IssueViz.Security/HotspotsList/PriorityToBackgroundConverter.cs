/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList
{
    [ValueConversion(typeof(HotspotPriority), typeof(Brush))]
    public class PriorityToBackgroundConverter : IValueConverter
    {
        internal static readonly SolidColorBrush HighPriorityBrush = new SolidColorBrush(Color.FromRgb(212, 51, 63));
        internal static readonly SolidColorBrush MediumPriorityBrush = new SolidColorBrush(Color.FromRgb(237, 125, 32));
        internal static readonly SolidColorBrush LowPriorityBrush = new SolidColorBrush(Color.FromRgb(234, 190, 6));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var priority = (HotspotPriority)value;

            switch (priority)
            {
                case HotspotPriority.High:
                    return HighPriorityBrush;
                case HotspotPriority.Medium:
                    return MediumPriorityBrush;
                case HotspotPriority.Low:
                    return LowPriorityBrush;
                default:
                    return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
