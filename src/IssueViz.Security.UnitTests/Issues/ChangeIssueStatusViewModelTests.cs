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

using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Issues;

[TestClass]
public class ChangeIssueStatusViewModelTests
{
    [TestMethod]
    public void Ctor_InitializesWithAllowedStatuses()
    {
        var allowedStatuses = new[] { ResolutionStatus.ACCEPT, ResolutionStatus.FALSE_POSITIVE };

        var testSubject = new ChangeIssueStatusViewModel(null, allowedStatuses);

        testSubject.AllStatusViewModels.Should().HaveCount(2);
        testSubject.AllStatusViewModels.Select(x => x.GetCurrentStatus<ResolutionStatus>())
            .Should().BeEquivalentTo(allowedStatuses);
    }

    [TestMethod]
    public void Ctor_InitializesWithCurrentStatus()
    {
        var currentStatus = ResolutionStatus.WONT_FIX;
        var allowedStatuses = new[] { ResolutionStatus.ACCEPT, ResolutionStatus.WONT_FIX, ResolutionStatus.FALSE_POSITIVE };

        var testSubject = new ChangeIssueStatusViewModel(currentStatus, allowedStatuses);

        testSubject.SelectedStatusViewModel.Should().NotBeNull();
        testSubject.SelectedStatusViewModel.GetCurrentStatus<ResolutionStatus>().Should().Be(currentStatus);
    }

    [TestMethod]
    public void Ctor_NoCurrentStatus_LeavesSelectionNull()
    {
        var allowedStatuses = new[] { ResolutionStatus.ACCEPT, ResolutionStatus.FALSE_POSITIVE };

        var testSubject = new ChangeIssueStatusViewModel(null, allowedStatuses);

        testSubject.SelectedStatusViewModel.Should().BeNull();
    }

    [TestMethod]
    public void GetNormalizedComment_NullComment_ReturnsNull()
    {
        var testSubject = new ChangeIssueStatusViewModel(null, [ResolutionStatus.ACCEPT]);
        testSubject.Comment = null;

        var result = testSubject.GetNormalizedComment();

        result.Should().BeNull();
    }

    [TestMethod]
    public void GetNormalizedComment_EmptyComment_ReturnsNull()
    {
        var testSubject = new ChangeIssueStatusViewModel(null, [ResolutionStatus.ACCEPT]);
        testSubject.Comment = "";

        var result = testSubject.GetNormalizedComment();

        result.Should().BeNull();
    }

    [TestMethod]
    public void GetNormalizedComment_WhitespaceComment_ReturnsNull()
    {
        var testSubject = new ChangeIssueStatusViewModel(null, [ResolutionStatus.ACCEPT]);
        testSubject.Comment = "   ";

        var result = testSubject.GetNormalizedComment();

        result.Should().BeNull();
    }

    [TestMethod]
    public void GetNormalizedComment_ValidComment_ReturnsTrimmed()
    {
        var testSubject = new ChangeIssueStatusViewModel(null, [ResolutionStatus.ACCEPT]);
        testSubject.Comment = "  test comment  ";

        var result = testSubject.GetNormalizedComment();

        result.Should().Be("test comment");
    }

    [TestMethod]
    public void StatusViewModels_ContainsCorrectTitlesAndDescriptions()
    {
        var allowedStatuses = new[] { ResolutionStatus.ACCEPT, ResolutionStatus.WONT_FIX, ResolutionStatus.FALSE_POSITIVE };

        var testSubject = new ChangeIssueStatusViewModel(null, allowedStatuses);

        var acceptVm = testSubject.AllStatusViewModels.First(x => x.GetCurrentStatus<ResolutionStatus>() == ResolutionStatus.ACCEPT);
        acceptVm.Title.Should().Be(Resources.ReviewIssueWindow_AcceptTitle);
        acceptVm.Description.Should().Be(Resources.ReviewIssueWindow_AcceptContent);

        var wontFixVm = testSubject.AllStatusViewModels.First(x => x.GetCurrentStatus<ResolutionStatus>() == ResolutionStatus.WONT_FIX);
        wontFixVm.Title.Should().Be(Resources.ReviewIssueWindow_WontFixTitle);
        wontFixVm.Description.Should().Be(Resources.ReviewIssueWindow_WontFixContent);

        var falsePositiveVm = testSubject.AllStatusViewModels.First(x => x.GetCurrentStatus<ResolutionStatus>() == ResolutionStatus.FALSE_POSITIVE);
        falsePositiveVm.Title.Should().Be(Resources.ReviewIssueWindow_FalsePositiveTitle);
        falsePositiveVm.Description.Should().Be(Resources.ReviewIssueWindow_FalsePositiveContent);
    }
}
