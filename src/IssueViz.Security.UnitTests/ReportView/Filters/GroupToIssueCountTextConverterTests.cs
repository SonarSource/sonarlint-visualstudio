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

using System.Collections.ObjectModel;
using System.Globalization;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Filters;

[TestClass]
public class GroupToIssueCountTextConverterTests
{
    private GroupToIssueCountTextConverter testSubject;
    private IGroupViewModel groupViewModel;

    [TestInitialize]
    public void TestInitialize()
    {
        groupViewModel = Substitute.For<IGroupViewModel>();
        testSubject = new GroupToIssueCountTextConverter();
    }

    [TestMethod]
    public void Convert_WhenValueIsNotGroupViewModel_ReturnsNull()
    {
        var result = testSubject.Convert("not a group", null, null, CultureInfo.InvariantCulture);
        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_WhenValueIsNull_ReturnsNull()
    {
        var result = testSubject.Convert(null, null, null, CultureInfo.InvariantCulture);
        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_WhenNoIssues_ReturnsExpectedText()
    {
        SetupGroup(0, 0);
        var result = testSubject.Convert(groupViewModel, null, null, CultureInfo.InvariantCulture);
        result.Should().Be("No findings");
    }

    [TestMethod]
    public void Convert_WhenOneFilteredAndOnePreFilteredIssue_ReturnsExpectedText()
    {
        SetupGroup(1, 1);
        var result = testSubject.Convert(groupViewModel, null, null, CultureInfo.InvariantCulture);
        result.Should().Be("1 finding");
    }

    [TestMethod]
    public void Convert_WhenMultipleFilteredAndPreFilteredIssues_ReturnsExpectedText()
    {
        SetupGroup(3, 5);
        var result = testSubject.Convert(groupViewModel, null, null, CultureInfo.InvariantCulture);
        result.Should().Be("3 of 5 findings");
    }

    [TestMethod]
    public void ConvertBack_Always_ThrowsNotImplementedException()
    {
        var action = () => testSubject.ConvertBack("value", null, null, CultureInfo.InvariantCulture);
        action.Should().Throw<NotImplementedException>();
    }

    private void SetupGroup(int filteredCount, int preFilteredCount)
    {
        var filtered = new ObservableCollection<IIssueViewModel>(CreateIssues(filteredCount));
        var preFiltered = CreateIssues(preFilteredCount);
        groupViewModel.FilteredIssues.Returns(filtered);
        groupViewModel.PreFilteredIssues.Returns(preFiltered);
    }

    private static IList<IIssueViewModel> CreateIssues(int count)
    {
        return Enumerable.Range(0, count).Select(_ => Substitute.For<IIssueViewModel>()).ToList();
    }
}
