/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks
{
    /// <summary>
    /// Converts a <see cref="DependencyRiskImpactSeverity"/> to a resource based on the severity and a suffix that can be provided via the converter parameter.
    /// </summary>
    [ValueConversion(typeof(DependencyRiskImpactSeverity), typeof(ImageSource))]
    public class DependencyRiskSeverityToResourceConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not DependencyRiskImpactSeverity severity || values[1] is not FrameworkElement element || values[2] is not IResourceFinder resourceFinder)
            {
                return null;
            }

            return resourceFinder.TryFindResource(element, $"{severity}{parameter}");
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
