/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Wrappers;

[TestClass]
public class WorkspaceChangeIndicatorTests
{
    [TestMethod]
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
        new WorkspaceChangeIndicator(Substitute.For<IFilesMissingFromRoslynSolution>()).IsChangeKindTrivial(kind).Should().Be(isTrivial);

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<WorkspaceChangeIndicator, IWorkspaceChangeIndicator>(
            MefTestHelpers.CreateExport<IFilesMissingFromRoslynSolution>());

    [TestMethod]
    [DataRow(WorkspaceChangeKind.DocumentAdded, true)]
    [DataRow(WorkspaceChangeKind.ProjectAdded, true)]
    [DataRow(WorkspaceChangeKind.ProjectChanged, true)]
    [DataRow(WorkspaceChangeKind.ProjectReloaded, true)]
    [DataRow(WorkspaceChangeKind.SolutionAdded, true)]
    [DataRow(WorkspaceChangeKind.SolutionChanged, true)]
    [DataRow(WorkspaceChangeKind.SolutionReloaded, true)]
    [DataRow(WorkspaceChangeKind.DocumentChanged, false)]
    [DataRow(WorkspaceChangeKind.DocumentRemoved, false)]
    [DataRow(WorkspaceChangeKind.ProjectRemoved, false)]
    [DataRow(WorkspaceChangeKind.SolutionRemoved, false)]
    public void MayAddMissingFiles_WithMissingFiles_DependsOnTheChangeKind(WorkspaceChangeKind kind, bool expected)
    {
        var missingFiles = Substitute.For<IFilesMissingFromRoslynSolution>();
        missingFiles.HasAny.Returns(true);

        new WorkspaceChangeIndicator(missingFiles).MayAddMissingFiles(kind).Should().Be(expected);
    }

    [TestMethod]
    public void MayAddMissingFiles_NoMissingFiles_IsFalse()
    {
        var missingFiles = Substitute.For<IFilesMissingFromRoslynSolution>();
        missingFiles.HasAny.Returns(false);

        new WorkspaceChangeIndicator(missingFiles).MayAddMissingFiles(WorkspaceChangeKind.DocumentAdded).Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void MissingFilesJoined_ReturnsWhetherMissingFilesAreNowInTheSolution(bool joined)
    {
        var missingFiles = Substitute.For<IFilesMissingFromRoslynSolution>();
        missingFiles.HasAny.Returns(true);
        var solution = Substitute.For<IRoslynSolutionWrapper>();
        missingFiles.RemoveFilesContainedIn(solution).Returns(joined);

        new WorkspaceChangeIndicator(missingFiles).MissingFilesJoined(solution).Should().Be(joined);
    }

    [TestMethod]
    public void MissingFilesJoined_NoMissingFiles_DoesNotLookThemUp()
    {
        var missingFiles = Substitute.For<IFilesMissingFromRoslynSolution>();
        missingFiles.HasAny.Returns(false);

        new WorkspaceChangeIndicator(missingFiles).MissingFilesJoined(Substitute.For<IRoslynSolutionWrapper>()).Should().BeFalse();

        missingFiles.DidNotReceiveWithAnyArgs().RemoveFilesContainedIn(default!);
    }

    [TestMethod]
    public void ForgetMissingFiles_ClearsThem()
    {
        var missingFiles = Substitute.For<IFilesMissingFromRoslynSolution>();

        new WorkspaceChangeIndicator(missingFiles).ForgetMissingFiles();

        missingFiles.Received(1).Clear();
    }
}
