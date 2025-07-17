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
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal class ChangeDependencyRiskStatusViewModel(DependencyRiskStatus currentStatus, IEnumerable<DependencyRiskStatus> allowedStatuses)
    : ChangeStatusViewModel<DependencyRiskStatus>(currentStatus, GetAllowedStatuses(allowedStatuses), showComment: true)
{
    private static readonly IReadOnlyList<StatusViewModel<DependencyRiskStatus>> AllDependencyRiskStatusViewModels =
    [
        new(DependencyRiskStatus.Open, DependencyRiskStatus.Open.ToString(),
            Resources.ChangeDependencyRiskStatus_OpenDescription, isCommentRequired: false),
        new(DependencyRiskStatus.Confirmed, DependencyRiskStatus.Confirmed.ToString(),
            Resources.ChangeDependencyRiskStatus_ConfirmedDescription, isCommentRequired: false),
        new(DependencyRiskStatus.Accepted, DependencyRiskStatus.Accepted.ToString(),
            Resources.ChangeDependencyRiskStatus_AcceptedDescription, isCommentRequired: true),
        new(DependencyRiskStatus.Safe, DependencyRiskStatus.Safe.ToString(),
            Resources.ChangeDependencyRiskStatus_SafeDescription, isCommentRequired: true)
    ];

    private static List<StatusViewModel<DependencyRiskStatus>> GetAllowedStatuses(IEnumerable<DependencyRiskStatus> allowedStatuses) =>
        AllDependencyRiskStatusViewModels.Where(vm => allowedStatuses.Any(hotspotStatus => vm.GetCurrentStatus<DependencyRiskStatus>() == hotspotStatus)).ToList();
}
