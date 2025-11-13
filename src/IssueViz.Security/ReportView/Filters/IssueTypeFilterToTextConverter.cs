/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

[ValueConversion(typeof(IIssueTypeFilterViewModel), typeof(string))]
public class IssueTypeFilterToTextConverter : IMultiValueConverter
{
    private const string PluralSuffix = "Plural";
    private const string SingularSuffix = "Singular";

    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not IIssueTypeFilterViewModel issueTypeFilterViewModel || values[1] is not IReportViewModel reportViewModel)
        {
            return null;
        }

        var count = reportViewModel.FilteredGroupViewModels.SelectMany(group => group.FilteredIssues).Count(vm => vm.IssueType == issueTypeFilterViewModel.IssueType);
        var totalCount = reportViewModel.AllGroupViewModels.SelectMany(group => group.PreFilteredIssues).Count(vm => vm.IssueType == issueTypeFilterViewModel.IssueType);
        return IssueCountHelper.FormatString(count, totalCount, GetResourceByKey(issueTypeFilterViewModel.IssueType + SingularSuffix), GetResourceByKey(issueTypeFilterViewModel.IssueType + PluralSuffix));
    }

    private static string GetResourceByKey(string resourceKey) => Resources.ResourceManager.GetString(resourceKey) ?? string.Empty;

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture) =>
        throw new NotImplementedException();
}
