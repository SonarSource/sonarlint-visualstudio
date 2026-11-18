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
    public void Add_NewFiles_RecordsThem()
    {
        testSubject.Add([File1]);

        testSubject.HasAny.Should().BeTrue();
    }

    [TestMethod]
    public void Add_SameFileWithDifferentCase_IsRecordedOnce()
    {
        testSubject.Add([File1, File1.ToUpperInvariant()]);

        testSubject.RemoveFilesContainedIn(CreateSolution(containing: File1)).Should().BeTrue();
        testSubject.HasAny.Should().BeFalse();
    }

    [TestMethod]
    public void RemoveFilesContainedIn_FileNowInTheSolution_RemovesOnlyThatFile()
    {
        testSubject.Add([File1, File2]);

        testSubject.RemoveFilesContainedIn(CreateSolution(containing: File1)).Should().BeTrue();

        testSubject.HasAny.Should().BeTrue();
        testSubject.RemoveFilesContainedIn(CreateSolution(containing: File2)).Should().BeTrue();
        testSubject.HasAny.Should().BeFalse();
    }

    [TestMethod]
    public void RemoveFilesContainedIn_FileStillMissing_ReturnsFalseAndKeepsIt()
    {
        testSubject.Add([File1]);

        testSubject.RemoveFilesContainedIn(CreateSolution(containing: File2)).Should().BeFalse();

        testSubject.HasAny.Should().BeTrue();
    }

    [TestMethod]
    public void Add_FileRetriedTheMaximumNumberOfTimes_IsNoLongerRecorded()
    {
        var solution = CreateSolution(containing: File1);
        for (var i = 0; i < FilesMissingFromRoslynSolution.MaxRetriesPerFile; i++)
        {
            testSubject.Add([File1]);
            testSubject.RemoveFilesContainedIn(solution).Should().BeTrue();
        }

        testSubject.Add([File1]);

        testSubject.HasAny.Should().BeFalse();
    }

    [TestMethod]
    public void Clear_ForgetsFilesAndRetries()
    {
        var solution = CreateSolution(containing: File1);
        for (var i = 0; i < FilesMissingFromRoslynSolution.MaxRetriesPerFile; i++)
        {
            testSubject.Add([File1]);
            testSubject.RemoveFilesContainedIn(solution);
        }
        testSubject.Add([File2]);

        testSubject.Clear();

        testSubject.HasAny.Should().BeFalse();
        testSubject.Add([File1]);
        testSubject.HasAny.Should().BeTrue();
    }

    private static IRoslynSolutionWrapper CreateSolution(string containing)
    {
        var solution = Substitute.For<IRoslynSolutionWrapper>();
        solution.ContainsCompilableDocument(Arg.Any<string>()).Returns(false);
        solution.ContainsCompilableDocument(containing).Returns(true);
        return solution;
    }
}
