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

using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class FileStateManagerTests
{
    private ILiveAnalysisStateFactory liveAnalysisStateFactory;
    private FileStateManager fileStateManager;

    [TestInitialize]
    public void TestInitialize()
    {
        liveAnalysisStateFactory = Substitute.For<ILiveAnalysisStateFactory>();
        fileStateManager = new FileStateManager(liveAnalysisStateFactory);
    }

    [TestMethod]
    public void Opened_FileState_CreatesAnalysisStateAndHandlesLiveAnalysis()
    {
        var fileState = CreateFileState();
        var analysisState = CreateLiveAnalysisState();
        liveAnalysisStateFactory.Create(fileState).Returns(analysisState);

        fileStateManager.Opened(fileState);

        liveAnalysisStateFactory.Received(1).Create(fileState);
        analysisState.Received(1).HandleLiveAnalysisEvent(false);
    }

    [TestMethod]
    public void Closed_FileState_RemovesStateAndDisposesAnalysisState()
    {
        var fileState = CreateFileState();
        var analysisState = CreateLiveAnalysisState();
        liveAnalysisStateFactory.Create(fileState).Returns(analysisState);

        fileStateManager.Opened(fileState);
        fileStateManager.Closed(fileState);

        analysisState.Received(1).Dispose();
        fileStateManager.GetAllStates().Should().BeEmpty();
    }

    [TestMethod]
    public void GetOpenDocuments_ReturnsCorrectDocuments()
    {
        var fileState1 = CreateFileState("file1.cs", AnalysisLanguage.RoslynFamily);
        var fileState2 = CreateFileState("file2.ts", AnalysisLanguage.TypeScript, AnalysisLanguage.CascadingStyleSheets);
        liveAnalysisStateFactory.Create(fileState1).Returns(CreateLiveAnalysisState());
        liveAnalysisStateFactory.Create(fileState2).Returns(CreateLiveAnalysisState());
        fileStateManager.Opened(fileState1);
        fileStateManager.Opened(fileState2);

        var documents = fileStateManager.GetOpenDocuments();

        documents.Should().HaveCount(2);
        documents.Select(d => d.FullPath).Should().BeEquivalentTo("file1.cs", "file2.ts");
        documents.Select(d => d.DetectedLanguages).Should().BeEquivalentTo(new AnalysisLanguage[][]{ [AnalysisLanguage.RoslynFamily], [AnalysisLanguage.TypeScript, AnalysisLanguage.CascadingStyleSheets]});
    }

    [TestMethod]
    public void AnalyzeAllOpenFiles_InvokesHandleBackgroundAnalysisEventOnAllStates()
    {
        var fileState1 = CreateFileState("file1.cs");
        var fileState2 = CreateFileState("file2.cs");
        var analysisState1 = CreateLiveAnalysisState();
        var analysisState2 = CreateLiveAnalysisState();
        liveAnalysisStateFactory.Create(fileState1).Returns(analysisState1);
        liveAnalysisStateFactory.Create(fileState2).Returns(analysisState2);
        fileStateManager.Opened(fileState1);
        fileStateManager.Opened(fileState2);

        fileStateManager.AnalyzeAllOpenFiles();

        analysisState1.Received(1).HandleBackgroundAnalysisEvent();
        analysisState2.Received(1).HandleBackgroundAnalysisEvent();
    }

    [TestMethod]
    public void Closed_FileStateNotOpened_DoesNothing()
    {
        var fileState = CreateFileState();

        Action act = () => fileStateManager.Closed(fileState);

        act.Should().NotThrow();
        fileStateManager.GetAllStates().Should().BeEmpty();
    }

    [TestMethod]
    public void Renamed_FileState_HandlesLiveAnalysisEventWithTrue()
    {
        var fileState = CreateFileState();
        var analysisState = CreateLiveAnalysisState();
        liveAnalysisStateFactory.Create(fileState).Returns(analysisState);
        fileStateManager.Opened(fileState);

        fileStateManager.Renamed(fileState);

        analysisState.Received(1).HandleLiveAnalysisEvent(true);
    }

    [TestMethod]
    public void ContentSaved_FileState_HandlesLiveAnalysisEventWithTrue()
    {
        var fileState = CreateFileState();
        var analysisState = CreateLiveAnalysisState();
        liveAnalysisStateFactory.Create(fileState).Returns(analysisState);
        fileStateManager.Opened(fileState);

        fileStateManager.ContentSaved(fileState);

        analysisState.Received(1).HandleLiveAnalysisEvent(true);
    }

    [TestMethod]
    public void ContentChanged_FileState_HandlesLiveAnalysisEventWithTrue()
    {
        var fileState = CreateFileState();
        var analysisState = CreateLiveAnalysisState();
        liveAnalysisStateFactory.Create(fileState).Returns(analysisState);
        fileStateManager.Opened(fileState);

        fileStateManager.ContentChanged(fileState);

        analysisState.Received(1).HandleLiveAnalysisEvent(true);
    }

    [TestMethod]
    public void HandleFileUpdate_FileStateNotOpened_CreatesAnalysisStateAndHandlesLiveAnalysis()
    {
        var fileState = CreateFileState();
        var analysisState = CreateLiveAnalysisState();
        liveAnalysisStateFactory.Create(fileState).Returns(analysisState);

        fileStateManager.ContentChanged(fileState);

        liveAnalysisStateFactory.Received(1).Create(fileState);
        analysisState.Received(1).HandleLiveAnalysisEvent(true);
    }

    private static IFileState CreateFileState(string filePath = "file.cs", params AnalysisLanguage[] languages)
    {
        var fileState = Substitute.For<IFileState>();
        fileState.FilePath.Returns(filePath);
        fileState.DetectedLanguages.Returns(languages);
        return fileState;
    }

    private ILiveAnalysisState CreateLiveAnalysisState()
    {
        var state = Substitute.For<ILiveAnalysisState>();
        return state;
    }
}
