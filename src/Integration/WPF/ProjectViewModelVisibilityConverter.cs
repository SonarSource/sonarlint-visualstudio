//-----------------------------------------------------------------------
// <copyright file="ProjectViewModelVisibilityConverter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
