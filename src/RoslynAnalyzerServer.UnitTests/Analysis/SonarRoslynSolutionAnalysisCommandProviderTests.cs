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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class SonarRoslynSolutionAnalysisCommandProviderTests
{
    private const string File1Cs = "file1.cs";
    private const string File2Cs = "file2.cs";
    private const string File3Cs = "file3.cs";
    private const string File4Cs = "file4.cs";
    private const string AnalyzedFile1Cs = "analyzedFile1.cs";
    private const string AnalyzedFile2Cs = "analyzedFile2.cs";
    private const string AnalyzedFile3Cs = "analyzedFile3.cs";
    private const string Project1 = "project1";
    private const string Project2 = "project2";
    private const string Project3 = "project3";
    private const string Project4 = "project4";

    private ISonarRoslynWorkspaceWrapper workspaceWrapper = null!;
    private TestLogger logger = null!;
    private ISonarRoslynSolutionWrapper solutionWrapper = null!;
    private SonarRoslynSolutionAnalysisCommandProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        workspaceWrapper = Substitute.For<ISonarRoslynWorkspaceWrapper>();
        logger = new TestLogger();
        solutionWrapper = Substitute.For<ISonarRoslynSolutionWrapper>();
        workspaceWrapper.GetCurrentSolution().Returns(solutionWrapper);
        testSubject = new SonarRoslynSolutionAnalysisCommandProvider(workspaceWrapper, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SonarRoslynSolutionAnalysisCommandProvider, ISonarRoslynSolutionAnalysisCommandProvider>(
            MefTestHelpers.CreateExport<ISonarRoslynWorkspaceWrapper>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<SonarRoslynSolutionAnalysisCommandProvider>();

    [TestMethod]
    public void GetAnalysisCommandsForCurrentSolution_NoProjects_ReturnsEmptyList()
    {
        solutionWrapper.Projects.Returns(new List<ISonarRoslynProjectWrapper>());

        var result = testSubject.GetAnalysisCommandsForCurrentSolution([File1Cs]);

        result.Should().BeEmpty();
        logger.AssertPartialOutputStringExists("No projects to analyze");
    }

    [TestMethod]
    public void GetAnalysisCommandsForCurrentSolution_ProjectDoesNotSupportCompilation_SkipsProject()
    {
        var project1 = CreateProject(Project1, false);
        solutionWrapper.Projects.Returns([project1]);

        var result = testSubject.GetAnalysisCommandsForCurrentSolution([File1Cs]);

        result.Should().BeEmpty();
        logger.AssertPartialOutputStringExists("Failed to get compilation for project: project1");
        logger.AssertPartialOutputStringExists("No projects to analyze");
    }

    [TestMethod]
    public void GetAnalysisCommandsForCurrentSolution_ProjectWithNoMatchingFiles_SkipsProject()
    {
        var project1 = CreateProject(Project1);
        project1.ContainsDocument(Arg.Any<string>(), out _).Returns(false);

        solutionWrapper.Projects.Returns([project1]);

        var result = testSubject.GetAnalysisCommandsForCurrentSolution([File1Cs]);

        result.Should().BeEmpty();
        logger.AssertPartialOutputStringExists("No files to analyze in project project1");
        logger.AssertPartialOutputStringExists("No projects to analyze");
    }

    [TestMethod]
    public void GetAnalysisCommandsForCurrentSolution_MultipleProjects_OnlyProjectWithFilesIsReturned()
    {
        var project1 = CreateProject(Project1);
        project1.ContainsDocument(Arg.Any<string>(), out _).Returns(false);
        var project2 = CreateProject(Project2);
        SetupContainsDocument(project2, File1Cs, AnalyzedFile1Cs);
        solutionWrapper.Projects.Returns([project1, project2]);

        var result = testSubject.GetAnalysisCommandsForCurrentSolution([File1Cs]);

        result.Should().ContainSingle();
        var command = result.Single();
        command.Project.Should().Be(project2);
        command.AnalysisCommands.OfType<SonarRoslynFileSyntaxAnalysis>().Should().HaveCount(1);
        command.AnalysisCommands.OfType<SonarRoslynFileSemanticAnalysis>().Should().HaveCount(1);
        logger.AssertPartialOutputStringExists("No files to analyze in project project1");
    }

    [TestMethod]
    public void GetAnalysisCommandsForCurrentSolution_MultipleMatchingFiles_AllFilesIncluded()
    {
        var project = CreateProject(Project1);
        SetupContainsDocument(project, File1Cs, AnalyzedFile1Cs);
        SetupContainsDocument(project, File2Cs, AnalyzedFile2Cs);
        project.ContainsDocument(File3Cs, out _).Returns(false);
        solutionWrapper.Projects.Returns([project]);

        var result = testSubject.GetAnalysisCommandsForCurrentSolution([File1Cs, File2Cs, File3Cs]);

        var command = result.Single();
        command.Project.Should().Be(project);
        command.AnalysisCommands.Should().HaveCount(4);
        ValidateContainsAllTypesOfAnalysisForFile(command, AnalyzedFile1Cs);
        ValidateContainsAllTypesOfAnalysisForFile(command, AnalyzedFile2Cs);
    }

    [TestMethod]
    public void GetAnalysisCommandsForCurrentSolution_MixedProjectResults_ReturnsCorrectProjects()
    {
        var projectWithNoCompilation = CreateProject(Project1, false);
        var projectWithNofiles = CreateProject(Project2);
        projectWithNofiles.ContainsDocument(Arg.Any<string>(), out _).Returns(false);
        var project3 = CreateProject(Project3);
        SetupContainsDocument(project3, File1Cs, AnalyzedFile1Cs);
        SetupContainsDocument(project3, File2Cs, AnalyzedFile2Cs);
        var project4 = CreateProject(Project4);
        SetupContainsDocument(project4, File3Cs, AnalyzedFile3Cs);
        SetupContainsDocument(project4, File1Cs, AnalyzedFile1Cs);
        solutionWrapper.Projects.Returns([projectWithNoCompilation, projectWithNofiles, project3, project4]);

        var result = testSubject.GetAnalysisCommandsForCurrentSolution([File1Cs, File2Cs, File3Cs, File4Cs]);

        result.Should().HaveCount(2);
        result[0].Project.Should().Be(project3);
        result[0].AnalysisCommands.Should().HaveCount(4);
        ValidateContainsAllTypesOfAnalysisForFile(result[0], AnalyzedFile1Cs);
        ValidateContainsAllTypesOfAnalysisForFile(result[0], AnalyzedFile2Cs);
        result[1].Project.Should().Be(project4);
        result[1].AnalysisCommands.Should().HaveCount(4);
        ValidateContainsAllTypesOfAnalysisForFile(result[1], AnalyzedFile3Cs);
        ValidateContainsAllTypesOfAnalysisForFile(result[1], AnalyzedFile1Cs);
        logger.AssertPartialOutputStringExists("Failed to get compilation for project: project1");
        logger.AssertPartialOutputStringExists("No files to analyze in project project2");
    }

    private void ValidateContainsAllTypesOfAnalysisForFile(SonarRoslynProjectAnalysisRequest request, string analysisFilePath)
    {
        ValidateContainsSyntacticAnalysisForFile(request, analysisFilePath);
        ValidateContainsSemanticAnalysisForFile(request, analysisFilePath);
    }

    private void ValidateContainsSyntacticAnalysisForFile(SonarRoslynProjectAnalysisRequest request, string analysisFilePath) =>
        request.AnalysisCommands.Any(x => x is SonarRoslynFileSyntaxAnalysis semanticAnalysis && semanticAnalysis.AnalysisFilePath == analysisFilePath).Should().BeTrue();

    private void ValidateContainsSemanticAnalysisForFile(SonarRoslynProjectAnalysisRequest request, string analysisFilePath) =>
        request.AnalysisCommands.Any(x => x is SonarRoslynFileSemanticAnalysis semanticAnalysis && semanticAnalysis.AnalysisFilePath == analysisFilePath).Should().BeTrue();


    private static ISonarRoslynProjectWrapper CreateProject(string projectName, bool supportsCompilation = true)
    {
        var project = Substitute.For<ISonarRoslynProjectWrapper>();
        project.Name.Returns(projectName);
        project.SupportsCompilation.Returns(supportsCompilation);
        return project;
    }

    private static void SetupContainsDocument(ISonarRoslynProjectWrapper project, string file, string analyzedFile) =>
        project.ContainsDocument(file, out Arg.Any<string?>()).Returns(x =>
        {
            x[1] = analyzedFile;
            return true;
        });
}
