/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.WPF
{
    internal sealed class ProjectViewModelVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 4)
            {
                return DependencyProperty.UnsetValue;
            }

            var projectName = values[0] as string;
            var showAllProjects = values[1] as bool?;
            var isBound = values[2] as bool?;
            var filterText = values[3] as string;

            if (projectName == null || showAllProjects == null || isBound == null || filterText == null)
            {
                return DependencyProperty.UnsetValue;
            }

            if (showAllProjects.Value)
            {
                return projectName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) != -1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                return isBound.Value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($"{nameof(ProjectViewModelVisibilityConverter)} does not support ConvertBack method.");
        }
    }
}
