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

using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Transition;

public static class SonarQubeIssueTransitionExtensions
{
    public static ResolutionStatus ToSlCoreResolutionStatus(this SonarQubeIssueTransition sonarQubeIssueTransition) =>
        sonarQubeIssueTransition switch
        {
            SonarQubeIssueTransition.FalsePositive => ResolutionStatus.FALSE_POSITIVE,
            SonarQubeIssueTransition.WontFix => ResolutionStatus.WONT_FIX,
            SonarQubeIssueTransition.Accept => ResolutionStatus.ACCEPT,
            _ => throw new ArgumentOutOfRangeException(nameof(sonarQubeIssueTransition), sonarQubeIssueTransition, null)
        };

    public static SonarQubeIssueTransition ToSonarQubeIssueTransition(this ResolutionStatus resolutionStatus) =>
        resolutionStatus switch
        {
            ResolutionStatus.FALSE_POSITIVE => SonarQubeIssueTransition.FalsePositive,
            ResolutionStatus.WONT_FIX => SonarQubeIssueTransition.WontFix,
            ResolutionStatus.ACCEPT => SonarQubeIssueTransition.Accept,
            _ => throw new ArgumentOutOfRangeException(nameof(resolutionStatus), resolutionStatus, null)
        };
}
