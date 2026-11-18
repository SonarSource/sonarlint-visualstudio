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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class FilesMissingFromRoslynSolutionTests
{
    private const string File1 = @"C:\src\file1.cs";
    private const string File2 = @"C:\src\file2.cs";

    private FilesMissingFromRoslynSolution testSubject = null!;

    [TestInitialize]
    public void TestInitialize() => testSubject = new FilesMissingFromRoslynSolution();

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<FilesMissingFromRoslynSolution, IFilesMissingFromRoslynSolution>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<FilesMissingFromRoslynSolution>();

    [TestMethod]
    public void HasAny_Initially_IsFalse() => testSubject.HasAny.Should().BeFalse();

    [TestMethod]
    public void Add_NewFiles_RecordsThemAndRaisesFilesAdded()
    {
        var raised = 0;
        testSubject.FilesAdded += (_, _) => raised++;

        testSubject.Add([File1]);

        testSubject.HasAny.Should().BeTrue();
        raised.Should().Be(1);
    }

    [TestMethod]
    public void Add_NoFilesOrOnlyKnownFiles_DoesNotRaiseFilesAdded()
    {
        testSubject.Add([File1]);
        var raised = 0;
        testSubject.FilesAdded += (_, _) => raised++;

        testSubject.Add([]);
        testSubject.Add([File1.ToUpperInvariant()]);

        raised.Should().Be(0);
    }

    [TestMethod]
    public void RemoveFilesContainedIn_FileNowInAProject_RemovesOnlyThatFile()
    {
        testSubject.Add([File1, File2]);
        var solution = CreateSolution(CreateProject(containing: File1));

        testSubject.RemoveFilesContainedIn(solution).Should().BeTrue();

        testSubject.HasAny.Should().BeTrue();
        testSubject.RemoveFilesContainedIn(CreateSolution(CreateProject(containing: File2))).Should().BeTrue();
        testSubject.HasAny.Should().BeFalse();
    }

    [TestMethod]
    public void RemoveFilesContainedIn_FileStillMissing_ReturnsFalseAndKeepsIt()
    {
        testSubject.Add([File1]);

        testSubject.RemoveFilesContainedIn(CreateSolution(CreateProject(containing: File2))).Should().BeFalse();

        testSubject.HasAny.Should().BeTrue();
    }

    [TestMethod]
    public void RemoveFilesContainedIn_FileOnlyInAProjectWithoutCompilation_KeepsIt()
    {
        testSubject.Add([File1]);

        testSubject.RemoveFilesContainedIn(CreateSolution(CreateProject(containing: File1, supportsCompilation: false))).Should().BeFalse();

        testSubject.HasAny.Should().BeTrue();
    }

    [TestMethod]
    public void Clear_ForgetsAllFiles()
    {
        testSubject.Add([File1, File2]);

        testSubject.Clear();

        testSubject.HasAny.Should().BeFalse();
    }

    private static IRoslynSolutionWrapper CreateSolution(params IRoslynProjectWrapper[] projects)
    {
        var solution = Substitute.For<IRoslynSolutionWrapper>();
        solution.Projects.Returns(projects);
        return solution;
    }

    private static IRoslynProjectWrapper CreateProject(string containing, bool supportsCompilation = true)
    {
        var project = Substitute.For<IRoslynProjectWrapper>();
        project.SupportsCompilation.Returns(supportsCompilation);
        project.ContainsDocument(Arg.Any<string>(), out Arg.Any<IRoslynDocumentWrapper?>()).Returns(false);
        project.ContainsDocument(containing, out Arg.Any<IRoslynDocumentWrapper?>()).Returns(x =>
        {
            x[1] = Substitute.For<IRoslynDocumentWrapper>();
            return true;
        });
        return project;
    }
}
