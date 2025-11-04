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
using System.Windows.Data;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

[ValueConversion(typeof(IGroupViewModel), typeof(string))]
public class GroupToIssueCountTextConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not IGroupViewModel groupViewModel)
        {
            return null;
        }

        return IssueCountHelper.FormatString(groupViewModel.FilteredIssues.Count, groupViewModel.PreFilteredIssues.Count, Resources.FindingSingularSmall, Resources.FindingPluralSmall);
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        throw new NotImplementedException();
}
