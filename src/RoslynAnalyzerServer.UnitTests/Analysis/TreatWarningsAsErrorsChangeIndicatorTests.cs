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

using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class TreatWarningsAsErrorsChangeIndicatorTests
{
    private TreatWarningsAsErrorsChangeIndicator testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new TreatWarningsAsErrorsChangeIndicator();
    }

    [TestMethod]
    [DataRow(WorkspaceChangeKind.SolutionAdded)]
    [DataRow(WorkspaceChangeKind.SolutionReloaded)]
    [DataRow(WorkspaceChangeKind.SolutionRemoved)]
    [DataRow(WorkspaceChangeKind.SolutionCleared)]
    public void RequiresFullSolutionUpdate_SolutionLevelChanges_ReturnsTrue(WorkspaceChangeKind changeKind)
    {
        var result = testSubject.RequiresFullSolutionUpdate(changeKind);

        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(WorkspaceChangeKind.SolutionChanged)]
    [DataRow(WorkspaceChangeKind.ProjectAdded)]
    [DataRow(WorkspaceChangeKind.ProjectRemoved)]
    [DataRow(WorkspaceChangeKind.ProjectChanged)]
    [DataRow(WorkspaceChangeKind.ProjectReloaded)]
    [DataRow(WorkspaceChangeKind.DocumentAdded)]
    [DataRow(WorkspaceChangeKind.DocumentChanged)]
    public void RequiresFullSolutionUpdate_OtherChanges_ReturnsFalse(WorkspaceChangeKind changeKind)
    {
        var result = testSubject.RequiresFullSolutionUpdate(changeKind);

        result.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(WorkspaceChangeKind.ProjectAdded)]
    [DataRow(WorkspaceChangeKind.ProjectChanged)]
    [DataRow(WorkspaceChangeKind.ProjectReloaded)]
    public void RequiresProjectUpdate_ProjectLevelChanges_ReturnsTrue(WorkspaceChangeKind changeKind)
    {
        var result = testSubject.RequiresProjectUpdate(changeKind);

        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(WorkspaceChangeKind.SolutionAdded)]
    [DataRow(WorkspaceChangeKind.SolutionReloaded)]
    [DataRow(WorkspaceChangeKind.SolutionChanged)]
    [DataRow(WorkspaceChangeKind.SolutionCleared)]
    [DataRow(WorkspaceChangeKind.SolutionRemoved)]
    [DataRow(WorkspaceChangeKind.ProjectRemoved)]
    [DataRow(WorkspaceChangeKind.DocumentAdded)]
    [DataRow(WorkspaceChangeKind.DocumentChanged)]
    public void RequiresProjectUpdate_OtherChanges_ReturnsFalse(WorkspaceChangeKind changeKind)
    {
        var result = testSubject.RequiresProjectUpdate(changeKind);

        result.Should().BeFalse();
    }
}
