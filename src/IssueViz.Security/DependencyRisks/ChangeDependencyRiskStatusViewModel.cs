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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal class ChangeDependencyRiskStatusViewModel(IEnumerable<DependencyRiskTransition> allowedStatuses)
    : ChangeStatusViewModel<DependencyRiskTransition>(null, GetAllowedStatuses(allowedStatuses), showComment: true)
{
    private static readonly IReadOnlyList<StatusViewModel<DependencyRiskTransition>> AllDependencyRiskStatusViewModels =
    [
        new(DependencyRiskTransition.Reopen, Resources.ChangeDependencyRiskStatus_ReopenTitle,
            Resources.ChangeDependencyRiskStatus_OpenDescription, isCommentRequired: false),
        new(DependencyRiskTransition.Confirm, Resources.ChangeDependencyRiskStatus_ConfirmTitle,
            Resources.ChangeDependencyRiskStatus_ConfirmedDescription, isCommentRequired: false),
        new(DependencyRiskTransition.Accept, Resources.ChangeDependencyRiskStatus_AcceptTitle,
            Resources.ChangeDependencyRiskStatus_AcceptedDescription, isCommentRequired: true),
        new(DependencyRiskTransition.Safe, Resources.ChangeDependencyRiskStatus_SafeTitle,
            Resources.ChangeDependencyRiskStatus_SafeDescription, isCommentRequired: true),
        new(DependencyRiskTransition.Fixed, Resources.ChangeDependencyRiskStatus_FixedTitle,
            Resources.ChangeDependencyRiskStatus_FixedDescription, isCommentRequired: false)
    ];

    public DependencyRiskTransition? GetSelectedTransition() => SelectedStatusViewModel?.GetCurrentStatus<DependencyRiskTransition>();

    private static List<StatusViewModel<DependencyRiskTransition>> GetAllowedStatuses(IEnumerable<DependencyRiskTransition> allowedStatuses) =>
        AllDependencyRiskStatusViewModels.Where(vm => allowedStatuses.Any(hotspotStatus => vm.GetCurrentStatus<DependencyRiskTransition>() == hotspotStatus)).ToList();
}
