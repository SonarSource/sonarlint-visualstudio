/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Windows;
using System.Windows.Data;

namespace SonarLint.VisualStudio.Core.WPF
{
    // Replacement for Microsoft.TeamFoundation.Controls.WPF.Converters.IntToVisibleConverter
    // (simplified version, similar to our BoolToVisibilityConverter)
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class IntToVisibleConverter : IValueConverter
    {
        public Visibility GreaterThanZeroVisibility { get; set; } = Visibility.Visible;

        public Visibility ZeroOrLessVisibility { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
            {
                throw new ArgumentException(nameof(targetType));
            }

            int? intValue = value as int?;
            if (intValue == null)
            {
                throw new ArgumentException(nameof(intValue));
            }

            bool isGreatThanZero = intValue.HasValue && intValue.Value > 0;
            return isGreatThanZero ? GreaterThanZeroVisibility : ZeroOrLessVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }
}
