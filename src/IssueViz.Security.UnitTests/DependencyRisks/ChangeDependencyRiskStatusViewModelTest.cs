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
    private readonly List<DependencyRiskTransition> statusWithMandatoryComment = [DependencyRiskTransition.Accept, DependencyRiskTransition.Safe];

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        List<DependencyRiskTransition> allowedTransitions = [DependencyRiskTransition.Reopen, DependencyRiskTransition.Confirm];

        var testSubject = new ChangeDependencyRiskStatusViewModel(allowedTransitions);

        testSubject.AllStatusViewModels.Should().HaveCount(allowedTransitions.Count);
        testSubject.AllStatusViewModels.Should().Contain(x => x.GetCurrentStatus<DependencyRiskTransition>() == DependencyRiskTransition.Reopen);
        testSubject.AllStatusViewModels.Should().Contain(x => x.GetCurrentStatus<DependencyRiskTransition>() == DependencyRiskTransition.Confirm);
        testSubject.ShowComment.Should().BeTrue();
    }

    [TestMethod]
    public void Ctor_AcceptAndSafeTransitionsHaveMandatoryComment()
    {
        List<DependencyRiskTransition> allTransitions = [DependencyRiskTransition.Reopen, DependencyRiskTransition.Confirm, DependencyRiskTransition.Accept, DependencyRiskTransition.Safe];

        var testSubject = new ChangeDependencyRiskStatusViewModel(allTransitions);

        testSubject.AllStatusViewModels.Should().HaveCount(allTransitions.Count);
        var mandatoryComments = GetViewModelsWithStatusesWithMandatoryComments(testSubject);
        mandatoryComments.Should().OnlyContain(vm => vm.IsCommentRequired);
        testSubject.AllStatusViewModels.Except(mandatoryComments).Should().OnlyContain(vm => !vm.IsCommentRequired);
    }

    [TestMethod]
    public void GetSelectedStatus_DefaultsToNull()
    {
        List<DependencyRiskTransition> allowedTransitions = [DependencyRiskTransition.Accept, DependencyRiskTransition.Safe];
        var testSubject = new ChangeDependencyRiskStatusViewModel(allowedTransitions);

        var result = testSubject.SelectedStatusViewModel.GetCurrentStatus<DependencyRiskTransition>();

        result.Should().BeNull();
    }

    [TestMethod]
    public void GetSelectedStatus_ReturnsSelectedStatus()
    {
        List<DependencyRiskTransition> allowedTransitions = [DependencyRiskTransition.Accept, DependencyRiskTransition.Safe];
        var testSubject = new ChangeDependencyRiskStatusViewModel(allowedTransitions);
        var acceptViewModel = testSubject.AllStatusViewModels.First(vm => vm.GetCurrentStatus<DependencyRiskTransition>() == DependencyRiskTransition.Accept);
        testSubject.SelectedStatusViewModel = acceptViewModel;

        var result = testSubject.SelectedStatusViewModel.GetCurrentStatus<DependencyRiskTransition>();

        result.Should().Be(DependencyRiskTransition.Accept);
    }

    private List<IStatusViewModel> GetViewModelsWithStatusesWithMandatoryComments(ChangeDependencyRiskStatusViewModel testSubject) =>
        testSubject.AllStatusViewModels.Where(vm => statusWithMandatoryComment.Contains(vm.GetCurrentStatus<DependencyRiskTransition>())).ToList();
}
