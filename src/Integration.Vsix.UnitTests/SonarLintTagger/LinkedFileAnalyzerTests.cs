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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class LinkedFileAnalyzerTests
{
    private IAnalysisStateProvider analysisStateProviderInstance;
    private ILogger logger;
    private IRoslynProjectWrapper project;
    private IRoslynSolutionWrapper solution;
    private LinkedFileAnalyzer testSubject;
    private ITypeReferenceFinder typeReferenceFinder;
    private IRoslynWorkspaceWrapper workspaceWrapper;

    [TestInitialize]
    public void TestInitialize()
    {
        typeReferenceFinder = Substitute.For<ITypeReferenceFinder>();
        workspaceWrapper = Substitute.For<IRoslynWorkspaceWrapper>();
        solution = Substitute.For<IRoslynSolutionWrapper>();
        project = Substitute.For<IRoslynProjectWrapper>();
        solution.Projects.Returns([project]);
        workspaceWrapper.GetCurrentSolution().Returns(solution);
        logger = Substitute.For<ILogger>();
        analysisStateProviderInstance = Substitute.For<IAnalysisStateProvider>();
        testSubject = new LinkedFileAnalyzer(typeReferenceFinder, new Lazy<IAnalysisStateProvider>(() => analysisStateProviderInstance), workspaceWrapper, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<LinkedFileAnalyzer, ILinkedFileAnalyzer>(
            MefTestHelpers.CreateExport<ITypeReferenceFinder>(),
            MefTestHelpers.CreateExport<IRoslynWorkspaceWrapper>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IAnalysisStateProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<LinkedFileAnalyzer>();

    [TestMethod]
    public void ScheduleLinkedAnalysis_FileNotInSolution_NoAnalysis()
    {
        var file = CreateFileState("missing.cs");
        SetupSolutionContainsFile(file, false);

        testSubject.ScheduleLinkedAnalysis(file, CancellationToken.None);

        typeReferenceFinder.DidNotReceiveWithAnyArgs().GetCrossFileReferencesInScopeAsync(default, default, default, default);
    }

    [TestMethod]
    public void ScheduleLinkedAnalysis_NoOtherFiles_NoAnalysis()
    {
        var file = CreateFileState("file.cs");
        var doc = Substitute.For<IRoslynDocumentWrapper>();
        SetupSolutionContainsFile(file, true, doc);
        var liveAnalysisState = CreateLiveAnalysisState(file);
        analysisStateProviderInstance.GetAllStates().Returns([liveAnalysisState]);
        SetupTypeReferenceFinderReturns(doc, [], []);

        testSubject.ScheduleLinkedAnalysis(file, CancellationToken.None);

        VerifyNotAnalyzed(liveAnalysisState);
    }

    [TestMethod]
    public void ScheduleLinkedAnalysis_OtherFilesNotInSolution_NoAnalysis()
    {
        var file = SetUpExisingFileInSolution("file.cs", out var doc, out var fileAnalysisState);
        SetUpFileNotInSolution("file.js", out var jsFileAnalysisState);
        SetupTypeReferenceFinderReturns(doc, [], []);
        analysisStateProviderInstance.GetAllStates().Returns([fileAnalysisState, jsFileAnalysisState]);

        testSubject.ScheduleLinkedAnalysis(file, CancellationToken.None);

        VerifyNotAnalyzed(fileAnalysisState);
        VerifyNotAnalyzed(jsFileAnalysisState);
    }

    [TestMethod]
    public void ScheduleLinkedAnalysis_OtherFileAlreadyBeingAnalyzed_NoAnalysis()
    {
        var file = SetUpExisingFileInSolution("file.cs", out var doc, out var fileAnalysisState);
        SetUpFileAlreadyAnalyzed("file2.cs", out var alreadyAnalyzedState);
        SetupTypeReferenceFinderReturns(doc, [], []);
        analysisStateProviderInstance.GetAllStates().Returns([fileAnalysisState, alreadyAnalyzedState]);

        testSubject.ScheduleLinkedAnalysis(file, CancellationToken.None);

        VerifyNotAnalyzed(fileAnalysisState);
        VerifyNotAnalyzed(alreadyAnalyzedState);
    }

    [TestMethod]
    public void ScheduleLinkedAnalysis_OtherFileNotLinked_NoAnalysis()
    {
        var file = SetUpExisingFileInSolution("file.cs", out var doc, out var fileAnalysisState);
        SetUpExisingFileInSolution("file2.cs", out var doc2, out var file2AnalysisState);
        SetupTypeReferenceFinderReturns(doc, [doc2], []);
        analysisStateProviderInstance.GetAllStates().Returns([fileAnalysisState, file2AnalysisState]);

        testSubject.ScheduleLinkedAnalysis(file, CancellationToken.None);

        VerifyNotAnalyzed(fileAnalysisState);
        VerifyNotAnalyzed(file2AnalysisState);
    }

    [TestMethod]
    public void ScheduleLinkedAnalysis_LinkedDocumentsFoundAndAnalyzed()
    {
        var file = SetUpExisingFileInSolution("file.cs", out var doc, out var fileAnalysisState);
        SetUpExisingFileInSolution("file2.cs", out var doc2, out var file2AnalysisState);
        SetupTypeReferenceFinderReturns(doc, [doc2], [doc2]);
        analysisStateProviderInstance.GetAllStates().Returns([fileAnalysisState, file2AnalysisState]);
        var cts = new CancellationTokenSource();

        testSubject.ScheduleLinkedAnalysis(file, cts.Token);

        VerifyNotAnalyzed(fileAnalysisState);
        file2AnalysisState.Received(1).HandleLiveAnalysisEvent(false);
        typeReferenceFinder.Received(1)
            .GetCrossFileReferencesInScopeAsync(Arg.Any<IRoslynDocumentWrapper>(), Arg.Any<IEnumerable<IRoslynDocumentWrapper>>(), Arg.Any<IRoslynSolutionWrapper>(), cts.Token);
    }

    [TestMethod]
    public void ScheduleLinkedAnalysis_Complex()
    {
        var file = SetUpExisingFileInSolution("file.cs", out var doc, out var fileAnalysisState);
        SetUpFileAlreadyAnalyzed("alreadyAnalyzed.cs", out var alreadyAnalyzed);
        SetUpExisingFileInSolution("file3.cs", out var doc3, out var file3AnalysisState);
        SetUpFileNotInSolution("script.js", out var jsFileAnalysisState);
        SetUpExisingFileInSolution("file5.cs", out var notLinkedDoc, out var notLinkedDocAnalysisState);
        SetUpExisingFileInSolution("file6.cs", out var doc6, out var file6AnalysisState);
        SetupTypeReferenceFinderReturns(doc, [doc3, notLinkedDoc, doc6], [doc3, doc6]);
        analysisStateProviderInstance.GetAllStates().Returns([fileAnalysisState, alreadyAnalyzed, file3AnalysisState, jsFileAnalysisState, notLinkedDocAnalysisState, file6AnalysisState]);

        testSubject.ScheduleLinkedAnalysis(file, CancellationToken.None);

        VerifyNotAnalyzed(fileAnalysisState);
        VerifyNotAnalyzed(alreadyAnalyzed);
        file3AnalysisState.Received(1).HandleLiveAnalysisEvent(false);
        VerifyNotAnalyzed(jsFileAnalysisState);
        VerifyNotAnalyzed(notLinkedDocAnalysisState);
        file6AnalysisState.Received(1).HandleLiveAnalysisEvent(false);
    }

    [TestMethod]
    public void ScheduleLinkedAnalysis_DoesNotThrow()
    {
        project.ContainsDocument(default, out _).ThrowsForAnyArgs(new Exception());

        var act = () => testSubject.ScheduleLinkedAnalysis(Substitute.For<IFileState>(), CancellationToken.None);

        act.Should().NotThrow();
    }

    private void SetupSolutionContainsFile(IFileState fileState, bool contains, IRoslynDocumentWrapper document = null) =>
        project.ContainsDocument(fileState.FilePath, out Arg.Any<IRoslynDocumentWrapper>())
            .Returns(x =>
            {
                x[1] = document;
                return contains;
            });

    private void SetupTypeReferenceFinderReturns(IRoslynDocumentWrapper doc, IEnumerable<IRoslynDocumentWrapper> toSearch, IEnumerable<IRoslynDocumentWrapper> docs) =>
        typeReferenceFinder.GetCrossFileReferencesInScopeAsync(
                doc,
                Arg.Is<IEnumerable<IRoslynDocumentWrapper>>(x => x.SequenceEqual(toSearch)),
                solution,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(docs.ToHashSet()));

    private IFileState CreateFileState(string filePath)
    {
        var fileState = Substitute.For<IFileState>();
        fileState.FilePath.Returns(filePath);
        return fileState;
    }

    private ILiveAnalysisState CreateLiveAnalysisState(IFileState fileState, bool isWaiting = true)
    {
        var state = Substitute.For<ILiveAnalysisState>();
        state.FileState.Returns(fileState);
        state.IsWaiting.Returns(isWaiting);
        return state;
    }

    private IFileState SetUpFileAlreadyAnalyzed(string name, out ILiveAnalysisState fileAnalysisState)
    {
        var file = CreateFileState(name);
        fileAnalysisState = CreateLiveAnalysisState(file, false);
        return file;
    }

    private IFileState SetUpFileNotInSolution(string name, out ILiveAnalysisState fileAnalysisState)
    {
        var file = CreateFileState(name);
        SetupSolutionContainsFile(file, false);
        fileAnalysisState = CreateLiveAnalysisState(file);
        return file;
    }

    private IFileState SetUpExisingFileInSolution(string fileName, out IRoslynDocumentWrapper doc, out ILiveAnalysisState fileAnalysisState)
    {
        var file = CreateFileState(fileName);
        doc = Substitute.For<IRoslynDocumentWrapper>();
        SetupSolutionContainsFile(file, true, doc);
        fileAnalysisState = CreateLiveAnalysisState(file);
        return file;
    }

    private void VerifyNotAnalyzed(ILiveAnalysisState state)
    {
        state.DidNotReceiveWithAnyArgs().HandleLiveAnalysisEvent(default);
        state.DidNotReceiveWithAnyArgs().HandleBackgroundAnalysisEvent();
    }
}
