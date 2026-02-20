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

using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class TreatWarningsAsErrorsCacheTests
{
    private TreatWarningsAsErrorsCache testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new TreatWarningsAsErrorsCache();
    }

    [TestMethod]
    public void IsTreatWarningsAsErrorsEnabled_ProjectNotInCache_ReturnsFalse()
    {
        var result = testSubject.IsTreatWarningsAsErrorsEnabled("UnknownProject");

        result.Should().BeFalse();
    }

    [TestMethod]
    public void UpdateForProject_SetsValueInCache()
    {
        testSubject.UpdateForProject("MyProject", true);

        testSubject.IsTreatWarningsAsErrorsEnabled("MyProject").Should().BeTrue();
    }

    [TestMethod]
    public void UpdateForProject_UpdatesExistingValue()
    {
        testSubject.UpdateForProject("MyProject", true);
        testSubject.UpdateForProject("MyProject", false);

        testSubject.IsTreatWarningsAsErrorsEnabled("MyProject").Should().BeFalse();
    }

    [TestMethod]
    public void IsTreatWarningsAsErrorsEnabled_IsCaseInsensitive()
    {
        testSubject.UpdateForProject("MyProject", true);

        testSubject.IsTreatWarningsAsErrorsEnabled("MYPROJECT").Should().BeTrue();
        testSubject.IsTreatWarningsAsErrorsEnabled("myproject").Should().BeTrue();
    }

    [TestMethod]
    public void UpdateFromSolution_PopulatesCacheFromProjects()
    {
        var project1 = CreateProjectWrapper("Project1", ReportDiagnostic.Error);
        var project2 = CreateProjectWrapper("Project2", ReportDiagnostic.Default);
        var project3 = CreateProjectWrapper("Project3", ReportDiagnostic.Warn);

        var solutionMock = new Mock<IRoslynSolutionWrapper>();
        solutionMock.Setup(s => s.Projects).Returns(new[] { project1, project2, project3 });

        testSubject.UpdateFromSolution(solutionMock.Object);

        testSubject.IsTreatWarningsAsErrorsEnabled("Project1").Should().BeTrue();
        testSubject.IsTreatWarningsAsErrorsEnabled("Project2").Should().BeFalse();
        testSubject.IsTreatWarningsAsErrorsEnabled("Project3").Should().BeFalse();
    }

    [TestMethod]
    public void UpdateFromSolution_ClearsPreviousCache()
    {
        testSubject.UpdateForProject("OldProject", true);

        var project = CreateProjectWrapper("NewProject", ReportDiagnostic.Error);
        var solutionMock = new Mock<IRoslynSolutionWrapper>();
        solutionMock.Setup(s => s.Projects).Returns(new[] { project });

        testSubject.UpdateFromSolution(solutionMock.Object);

        testSubject.IsTreatWarningsAsErrorsEnabled("OldProject").Should().BeFalse();
        testSubject.IsTreatWarningsAsErrorsEnabled("NewProject").Should().BeTrue();
    }

    [TestMethod]
    public void UpdateFromSolution_EmptySolution_ClearsCache()
    {
        testSubject.UpdateForProject("MyProject", true);

        var solutionMock = new Mock<IRoslynSolutionWrapper>();
        solutionMock.Setup(s => s.Projects).Returns(Array.Empty<IRoslynProjectWrapper>());

        testSubject.UpdateFromSolution(solutionMock.Object);

        testSubject.IsTreatWarningsAsErrorsEnabled("MyProject").Should().BeFalse();
    }

    private static IRoslynProjectWrapper CreateProjectWrapper(string name, ReportDiagnostic? generalDiagnosticOption)
    {
        var projectMock = new Mock<IRoslynProjectWrapper>();
        projectMock.Setup(p => p.Name).Returns(name);
        projectMock.Setup(p => p.GeneralDiagnosticOption).Returns(generalDiagnosticOption);
        return projectMock.Object;
    }
}
