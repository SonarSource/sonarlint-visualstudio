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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class ChangeDependencyRiskStatusViewModelTest
{
    private readonly List<DependencyRiskStatus> statusWithMandatoryComment = [DependencyRiskStatus.Accepted, DependencyRiskStatus.Safe];

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        var currentStatus = DependencyRiskStatus.Open;
        List<DependencyRiskStatus> allowedStatuses = [DependencyRiskStatus.Open, DependencyRiskStatus.Confirmed];

        var testSubject = new ChangeDependencyRiskStatusViewModel(currentStatus, allowedStatuses);

        testSubject.AllStatusViewModels.Should().HaveCount(allowedStatuses.Count);
        testSubject.AllStatusViewModels.Should().Contain(x => x.GetCurrentStatus<DependencyRiskStatus>() == DependencyRiskStatus.Open);
        testSubject.AllStatusViewModels.Should().Contain(x => x.GetCurrentStatus<DependencyRiskStatus>() == DependencyRiskStatus.Confirmed);
        testSubject.SelectedStatusViewModel.GetCurrentStatus<DependencyRiskStatus>().Should().Be(currentStatus);
        testSubject.ShowComment.Should().BeTrue();
    }

    [TestMethod]
    public void Ctor_AcceptedAndSafeStatusHaveMandatoryComment()
    {
        var allStatuses = Enum.GetValues(typeof(DependencyRiskStatus)).Cast<DependencyRiskStatus>().ToList();

        var testSubject = new ChangeDependencyRiskStatusViewModel(default, allStatuses);

        testSubject.AllStatusViewModels.Should().HaveCount(allStatuses.Count);
        var mandatoryComments = GetViewModelsWithStatusesWithMandatoryComments(testSubject);
        mandatoryComments.Should().OnlyContain(vm => vm.IsCommentRequired);
        testSubject.AllStatusViewModels.Except(mandatoryComments).Should().OnlyContain(vm => !vm.IsCommentRequired);
    }

    [TestMethod]
    public void SelectedStatus_IsInListOfAllowedStatuses_InitializesCorrectly()
    {
        var currentStatus = DependencyRiskStatus.Accepted;
        List<DependencyRiskStatus> allowedStatuses = [DependencyRiskStatus.Safe, DependencyRiskStatus.Accepted];

        var testSubject = new ChangeDependencyRiskStatusViewModel(currentStatus, allowedStatuses);

        testSubject.SelectedStatusViewModel.GetCurrentStatus<DependencyRiskStatus>().Should().Be(currentStatus);
    }

    [TestMethod]
    public void SelectedStatus_IsNotInListOfAllowedStatuses_InitializesNull()
    {
        var currentStatus = DependencyRiskStatus.Open;
        List<DependencyRiskStatus> allowedStatuses = [DependencyRiskStatus.Safe, DependencyRiskStatus.Accepted];

        var testSubject = new ChangeDependencyRiskStatusViewModel(currentStatus, allowedStatuses);

        testSubject.SelectedStatusViewModel.Should().BeNull();
    }

    private List<IStatusViewModel> GetViewModelsWithStatusesWithMandatoryComments(ChangeDependencyRiskStatusViewModel testSubject) =>
        testSubject.AllStatusViewModels.Where(vm => statusWithMandatoryComment.Contains(vm.GetCurrentStatus<DependencyRiskStatus>())).ToList();
}
