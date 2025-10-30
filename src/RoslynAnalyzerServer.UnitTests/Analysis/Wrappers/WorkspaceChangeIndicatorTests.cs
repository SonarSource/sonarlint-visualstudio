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
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Wrappers;

[TestClass]
public class WorkspaceChangeIndicatorTests
{
    [DataTestMethod]
    [DataRow(WorkspaceChangeKind.DocumentAdded, true)]
    [DataRow(WorkspaceChangeKind.DocumentRemoved, true)]
    [DataRow(WorkspaceChangeKind.DocumentReloaded, true)]
    [DataRow(WorkspaceChangeKind.DocumentChanged, true)]
    [DataRow(WorkspaceChangeKind.DocumentInfoChanged, true)]
    [DataRow(WorkspaceChangeKind.AdditionalDocumentAdded, true)]
    [DataRow(WorkspaceChangeKind.AdditionalDocumentRemoved, true)]
    [DataRow(WorkspaceChangeKind.AdditionalDocumentReloaded, true)]
    [DataRow(WorkspaceChangeKind.AdditionalDocumentChanged, true)]
    [DataRow(WorkspaceChangeKind.AnalyzerConfigDocumentAdded, true)]
    [DataRow(WorkspaceChangeKind.AnalyzerConfigDocumentRemoved, true)]
    [DataRow(WorkspaceChangeKind.AnalyzerConfigDocumentReloaded, true)]
    [DataRow(WorkspaceChangeKind.AnalyzerConfigDocumentChanged, true)]
    [DataRow(WorkspaceChangeKind.SolutionAdded, false)]
    [DataRow(WorkspaceChangeKind.SolutionRemoved, false)]
    [DataRow(WorkspaceChangeKind.SolutionChanged, false)]
    [DataRow(WorkspaceChangeKind.ProjectAdded, false)]
    [DataRow(WorkspaceChangeKind.ProjectRemoved, false)]
    [DataRow(WorkspaceChangeKind.ProjectChanged, false)]
    public void IsChangeKindTrivial_ReturnsExpectedResult(WorkspaceChangeKind kind, bool isTrivial) =>
        new WorkspaceChangeIndicator().IsChangeKindTrivial(kind).Should().Be(isTrivial);
}
