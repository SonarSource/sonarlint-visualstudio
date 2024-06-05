/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class VsProjectInfoProviderTest
{
    private const string FilePath = "file/path";
    private const string ProjectName = "projeccct";
    private static readonly Guid ProjectGuid = Guid.NewGuid();

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<VsProjectInfoProvider, IVsProjectInfoProvider>(
            MefTestHelpers.CreateExport<SVsServiceProvider>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<VsProjectInfoProvider>();
    }

    [TestMethod]
    public async Task NoProjectItem_ReturnsNone()
    {
        var dte = SetUpDte(SetUpSolution(null));
        var solution = Substitute.For<IVsSolution5>();
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var result = await testSubject.GetDocumentProjectInfoAsync(document);
        
        result.Should().BeEquivalentTo(("{none}", Guid.Empty));
        testLogger.AssertNoOutputMessages();
        solution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }
    
    [TestMethod]
    public async Task NoProject_ReturnsNone()
    {
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(null)));
        var solution = Substitute.For<IVsSolution5>();
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var result = await testSubject.GetDocumentProjectInfoAsync(document);
        
        result.Should().BeEquivalentTo(("{none}", Guid.Empty));
        testLogger.AssertNoOutputMessages();
        solution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }
    
    [TestMethod]
    public async Task NoProjectNameAndGuid_ReturnsNone()
    {
        var project = Substitute.For<Project>();
        project.Name.Returns((string)null);
        project.FileName.Returns((string)null);
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(project)));
        var solution = Substitute.For<IVsSolution5>();
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var result = await testSubject.GetDocumentProjectInfoAsync(document);
        
        result.Should().BeEquivalentTo(("{none}", Guid.Empty));
        testLogger.AssertNoOutputMessages();
        solution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }
    
    [TestMethod]
    public async Task NoProjectNameAndGuid_ReturnsNoneWithGuid()
    {
        var project = Substitute.For<Project>();
        project.Name.Returns((string)null);
        project.FileName.Returns("someprojectfile.csproj");
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(project)));
        var solution = SetUpProjectGuid("someprojectfile", ProjectGuid);
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var result = await testSubject.GetDocumentProjectInfoAsync(document);
        
        result.Should().BeEquivalentTo(("{none}", ProjectGuid));
        testLogger.AssertNoOutputMessages();
    }
    
    [TestMethod]
    public async Task NoProjectFile_ReturnsNameOnly()
    {
        var project = Substitute.For<Project>();
        project.Name.Returns(ProjectName);
        project.FileName.Returns((string)null);
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(project)));
        var solution = Substitute.For<IVsSolution5>();
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var result = await testSubject.GetDocumentProjectInfoAsync(document);
        
        result.Should().BeEquivalentTo((ProjectName, Guid.Empty));
        testLogger.AssertNoOutputMessages();
        solution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }
    
    [TestMethod]
    public async Task CorrectProject_ReturnsNameAndGuid()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var solution = SetUpProjectGuid(ProjectName, ProjectGuid);
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var result = await testSubject.GetDocumentProjectInfoAsync(document);
        
        result.Should().BeEquivalentTo((ProjectName, ProjectGuid));
        testLogger.AssertNoOutputMessages();
    }
    
    [TestMethod]
    public async Task ProjectGuidThrows_NonCriticalException_Logs()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var solution = Substitute.For<IVsSolution5>();
        solution
            .GetGuidOfProjectFile(default)
            .ThrowsForAnyArgs(new InvalidOperationException());
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var act = async() => await testSubject.GetDocumentProjectInfoAsync(document);

        await act.Should().NotThrowAsync();
        testLogger.AssertPartialOutputStringExists("Failed to calculate project guid of file");
    }
    
    [TestMethod]
    public async Task ProjectGuidThrows_CriticalException_DoesNotCatch()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var solution = Substitute.For<IVsSolution5>();
        solution
            .GetGuidOfProjectFile(default)
            .ThrowsForAnyArgs(new DivideByZeroException());
        var document = SetUpDocument();
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(solution, dte, testLogger);

        var act = async() => await testSubject.GetDocumentProjectInfoAsync(document);

        await act.Should().ThrowAsync<DivideByZeroException>();
    }

    private ITextDocument SetUpDocument()
    {
        var textDocument = Substitute.For<ITextDocument>();
        textDocument.FilePath.Returns(FilePath);
        return textDocument;
    }

    private IVsProjectInfoProvider CreateTestSubject(IVsSolution5 vsSolution, DTE2 dte, ILogger logger = null, IThreadHandling threadHandling = null)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(SDTE)).Returns(dte);
        serviceProvider.GetService(typeof(SVsSolution)).Returns(vsSolution);
        logger ??= new TestLogger();
        threadHandling ??= new NoOpThreadHandler();
        return new VsProjectInfoProvider(serviceProvider, logger, threadHandling);
    }

    private DTE2 SetUpCorrectDte(string projectName) =>
        SetUpDte(SetUpSolution(SetUpProjectItem(SetUpProject(projectName))));

    private Project SetUpProject(string projectName)
    {
        var mockProject = Substitute.For<Project>();
        mockProject.Name.Returns(projectName);
        mockProject.FileName.Returns($"{projectName}.csproj");
        return mockProject;
    }

    private ProjectItem SetUpProjectItem(Project project)
    {
        var mockProjectItem = Substitute.For<ProjectItem>();
        mockProjectItem.ContainingProject.Returns(project);
        return mockProjectItem;
    }

    private Solution SetUpSolution(ProjectItem projectItem)
    {
        var mockSolution = Substitute.For<Solution>();
        mockSolution
            .FindProjectItem(Arg.Any<string>())
            .Returns(projectItem);
        return mockSolution;
    }

    private DTE2 SetUpDte(Solution solution)
    {
        var mockDTE = Substitute.For<DTE2>();
        mockDTE.Solution.Returns(solution);
        return mockDTE;
    }

    private IVsSolution5 SetUpProjectGuid(string projectName, Guid projectGuid)
    {
        var mockVsSolution = Substitute.For<IVsSolution5>();
        mockVsSolution.GetGuidOfProjectFile($"{projectName}.csproj")
            .Returns(projectGuid);
        return mockVsSolution;
    }
}
