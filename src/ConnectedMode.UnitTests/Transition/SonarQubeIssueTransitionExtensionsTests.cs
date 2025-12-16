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

using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Transition;

[TestClass]
public class SonarQubeIssueTransitionExtensionsTests
{
    [TestMethod]
    [DataRow(SonarQubeIssueTransition.Accept, ResolutionStatus.ACCEPT)]
    [DataRow(SonarQubeIssueTransition.WontFix, ResolutionStatus.WONT_FIX)]
    [DataRow(SonarQubeIssueTransition.FalsePositive, ResolutionStatus.FALSE_POSITIVE)]
    public void ToResolutionStatus_FalsePositive(SonarQubeIssueTransition transition, ResolutionStatus expectedStatus) => transition.ToSlCoreResolutionStatus().Should().Be(expectedStatus);

    [TestMethod]
    public void ToResolutionStatus_InvalidTransition_Throws()
    {
        var act = () => ((SonarQubeIssueTransition)int.MaxValue).ToSlCoreResolutionStatus();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    [DataRow(ResolutionStatus.ACCEPT, SonarQubeIssueTransition.Accept)]
    [DataRow(ResolutionStatus.WONT_FIX, SonarQubeIssueTransition.WontFix)]
    [DataRow(ResolutionStatus.FALSE_POSITIVE, SonarQubeIssueTransition.FalsePositive)]
    public void ToSonarQubeIssueTransition_FalsePositive(ResolutionStatus resolutionStatus, SonarQubeIssueTransition expectedTransition) =>
        resolutionStatus.ToSonarQubeIssueTransition().Should().Be(expectedTransition);

    [TestMethod]
    public void ToSonarQubeIssueTransition_InvalidResolutionStatus_Throws()
    {
        var act = () => ((ResolutionStatus)int.MaxValue).ToSonarQubeIssueTransition();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
