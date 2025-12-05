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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
[Ignore]
public class RoslynSolutionAnalysisCommandProviderTests
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

    private IRoslynWorkspaceWrapper workspaceWrapper = null!;
    private TestLogger logger = null!;
    private IRoslynSolutionWrapper solutionWrapper = null!;
    private RoslynSolutionAnalysisCommandProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        workspaceWrapper = Substitute.For<IRoslynWorkspaceWrapper>();
        logger = Substitute.ForPartsOf<TestLogger>();
        solutionWrapper = Substitute.For<IRoslynSolutionWrapper>();
        workspaceWrapper.GetCurrentSolution().Returns(solutionWrapper);
        testSubject = new RoslynSolutionAnalysisCommandProvider(workspaceWrapper, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynSolutionAnalysisCommandProvider, IRoslynSolutionAnalysisCommandProvider>(
            MefTestHelpers.CreateExport<IRoslynWorkspaceWrapper>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynSolutionAnalysisCommandProvider>();

    [TestMethod]
    public void Ctor_SetsLogContext() =>
        logger.Received(1).ForContext(Resources.RoslynLogContext, Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisConfigurationLogContext);

    [TestMethod]
    public void GetAnalysisCommandsForCurrentSolution_NoProjects_ReturnsEmptyList()
    {
        solutionWrapper.Projects.Returns(new List<IRoslynProjectWrapper>());

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
        logger.AssertPartialOutputStringExists("Project project1 does not support compilation");
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
        command.AnalysisCommands.OfType<RoslynFileSyntaxAnalysis>().Should().HaveCount(1);
        command.AnalysisCommands.OfType<RoslynFileSemanticAnalysis>().Should().HaveCount(1);
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
        logger.AssertPartialOutputStringExists("Project project1 does not support compilation");
    }

    private void ValidateContainsAllTypesOfAnalysisForFile(RoslynProjectAnalysisRequest request, string analysisFilePath)
    {
        ValidateContainsSyntacticAnalysisForFile(request, analysisFilePath);
        ValidateContainsSemanticAnalysisForFile(request, analysisFilePath);
    }

    private void ValidateContainsSyntacticAnalysisForFile(RoslynProjectAnalysisRequest request, string analysisFilePath) =>
        request.AnalysisCommands.Any(x => x is RoslynFileSyntaxAnalysis semanticAnalysis && semanticAnalysis.AnalysisFilePath == analysisFilePath).Should().BeTrue();

    private void ValidateContainsSemanticAnalysisForFile(RoslynProjectAnalysisRequest request, string analysisFilePath) =>
        request.AnalysisCommands.Any(x => x is RoslynFileSemanticAnalysis semanticAnalysis && semanticAnalysis.AnalysisFilePath == analysisFilePath).Should().BeTrue();


    private static IRoslynProjectWrapper CreateProject(string projectName, bool supportsCompilation = true)
    {
        var project = Substitute.For<IRoslynProjectWrapper>();
        project.Name.Returns(projectName);
        project.SupportsCompilation.Returns(supportsCompilation);
        return project;
    }

    private static void SetupContainsDocument(IRoslynProjectWrapper project, string file, string analyzedFile) =>
        project.ContainsDocument(file, out Arg.Any<IRoslynDocumentWrapper?>()).Returns(x =>
        {
            var roslynDocumentWrapper = Substitute.For<IRoslynDocumentWrapper>();
            roslynDocumentWrapper.FilePath.Returns(analyzedFile);
            x[1] = roslynDocumentWrapper;
            return true;
        });
}
