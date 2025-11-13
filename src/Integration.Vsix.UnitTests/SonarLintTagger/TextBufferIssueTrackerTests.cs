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

using System.Text;
using System.Windows.Documents;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.TestInfrastructure.Editor;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class TextBufferIssueTrackerTests
{
    private const string InitialFilePath = "foo.js";
    private const string TextContent = "file content";
    private readonly AnalysisLanguage[] javascriptLanguage = [AnalysisLanguage.Javascript];
    private readonly AnalysisLanguage[] typescriptLanguage = [AnalysisLanguage.TypeScript];
    private readonly AnalysisLanguage[] csharpLanguage = [AnalysisLanguage.RoslynFamily];
    private TestLogger logger;
    private ITextSnapshot initialTextSnapshot;
    private IContentType initialMockContentType;
    private ITextBuffer mockDocumentTextBuffer;
    private ITextDocument mockedJavascriptDocumentFooJs;
    private ISonarErrorListDataSource mockSonarErrorDataSource;
    private ISonarLanguageRecognizer languageRecognizer;
    private IDocumentTrackerUpdater documentTrackerUpdater;
    private TextBufferIssueTracker testSubject;
    private IVsProjectInfoProvider vsProjectInfoProvider;
    private IIssueConsumerFactory issueConsumerFactory;
    private IIssueConsumerStorage issueConsumerStorage;
    private readonly (string projectName, Guid projectGuid) initialProjectInfo = (projectName: "project 1", projectGuid: Guid.NewGuid());

    [TestInitialize]
    public void SetUp()
    {
        logger = Substitute.ForPartsOf<TestLogger>();
        mockSonarErrorDataSource = Substitute.For<ISonarErrorListDataSource>();
        languageRecognizer = Substitute.For<ISonarLanguageRecognizer>();
        vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        documentTrackerUpdater = Substitute.For<IDocumentTrackerUpdater>();
        initialMockContentType = Substitute.For<IContentType>();
        initialTextSnapshot = CreateTextSnapshotMock();
        mockDocumentTextBuffer = CreateTextBufferMock(initialTextSnapshot, initialMockContentType);
        mockedJavascriptDocumentFooJs = CreateDocumentMock(InitialFilePath, mockDocumentTextBuffer);
        MockGetDocumentProjectInfo(mockedJavascriptDocumentFooJs.FilePath, initialProjectInfo);
        languageRecognizer.Detect(mockedJavascriptDocumentFooJs.FilePath, initialMockContentType).Returns(javascriptLanguage);
        testSubject =  new(documentTrackerUpdater,
            mockedJavascriptDocumentFooJs, languageRecognizer,
            mockSonarErrorDataSource, vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage, logger);;
    }

    [TestMethod]
    [Description("TextBufferIssueTracker is no longer used as a real tagger and therefore should not produce any tags")]
    public void GetTags_EmptyArray() => testSubject.GetTags(null).Should().BeEmpty();

    [TestMethod]
    public void Ctor_SetsContext()
    {
        logger.Received(1).ForContext(nameof(TextBufferIssueTracker));
    }

    [TestMethod]
    public void Ctor_RegistersEventsTrackerAndFactory()
    {
        CheckFactoryWasRegisteredWithDataSource(testSubject.Factory, times: 1);

        mockedJavascriptDocumentFooJs.Received(1).FileActionOccurred += Arg.Any<EventHandler<TextDocumentFileActionEventArgs>>();
        ((ITextBuffer2)mockDocumentTextBuffer).Received(1).ChangedOnBackground += Arg.Any<EventHandler<TextContentChangedEventArgs>>();
    }

    [TestMethod]
    public void Ctor_CreatesFactoryWithCorrectData()
    {
        var issuesSnapshot = testSubject.Factory.CurrentSnapshot;
        issuesSnapshot.Should().NotBeNull();

        issuesSnapshot.AnalyzedFilePath.Should().Be(mockedJavascriptDocumentFooJs.FilePath);
        issuesSnapshot.Issues.Count().Should().Be(0);
    }

    [TestMethod]
    public void Ctor_UpdatesAnalysisStateAndCreatesIssueConsumer()
    {
        documentTrackerUpdater.Received(1).OnDocumentOpened(testSubject);
        VerifyMetadataUpdated(mockedJavascriptDocumentFooJs.FilePath, initialMockContentType, javascriptLanguage);
        VerifyIssueConsumerNotCreated();
    }

    [TestMethod]
    public void Dispose_CleansUpEventsAndRegistrations()
    {
        var singletonManager = new SingletonDisposableTaggerManager<IErrorTag>(null);
        mockDocumentTextBuffer.Properties.AddProperty(TaggerProvider.SingletonManagerPropertyCollectionKey, singletonManager);
        VerifySingletonManagerExists(mockDocumentTextBuffer);
        CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, times: 0);

        testSubject.Dispose();

        documentTrackerUpdater.Received(1).OnDocumentClosed(testSubject);
        VerifyIssueConsumerRemoved(InitialFilePath);
        VerifySingletonManagerDoesNotExist(mockDocumentTextBuffer);
        CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, times: 1);
        mockedJavascriptDocumentFooJs.Received(1).FileActionOccurred -= Arg.Any<EventHandler<TextDocumentFileActionEventArgs>>();
        ((ITextBuffer2)mockDocumentTextBuffer).Received(1).ChangedOnBackground -= Arg.Any<EventHandler<TextContentChangedEventArgs>>();
    }

    [TestMethod]
    public void WhenFileIsSaved_FileSnapshotIsUpdated()
    {
        var csharpContentType = Substitute.For<IContentType>();
        mockedJavascriptDocumentFooJs.TextBuffer.ContentType.Returns(csharpContentType);
        languageRecognizer.Detect(mockedJavascriptDocumentFooJs.FilePath, csharpContentType).Returns(csharpLanguage);
        ClearMocks();

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);

        documentTrackerUpdater.Received(1).OnDocumentSaved(testSubject);
        VerifyMetadataUpdated(mockedJavascriptDocumentFooJs.FilePath, csharpContentType, csharpLanguage);
        VerifyIssueConsumerNotCreated();
        VerifyIssueConsumerNotRemoved();
    }

    [TestMethod]
    public void WhenFileIsLoaded_EventsAreNotRaised()
    {
        documentTrackerUpdater.ClearReceivedCalls();

        RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);

        documentTrackerUpdater.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void WhenFileIsRenamed_LastAnalysisFilePathIsUpdated()
    {
        var newFilePath = "newName.ts";
        var newTypescriptContentType = Substitute.For<IContentType>();
        languageRecognizer.Detect(newFilePath, newTypescriptContentType).Returns(typescriptLanguage);
        mockedJavascriptDocumentFooJs.FilePath.Returns(newFilePath);
        mockedJavascriptDocumentFooJs.TextBuffer.ContentType.Returns(newTypescriptContentType);

        RaiseFileRenamedEvent(mockedJavascriptDocumentFooJs, newFilePath);

        testSubject.FilePath.Should().Be(newFilePath);
        documentTrackerUpdater.Received(1).OnOpenDocumentRenamed(testSubject, InitialFilePath);
        VerifyMetadataUpdated(newFilePath, newTypescriptContentType, typescriptLanguage);
        VerifyIssueConsumerNotCreated();
        VerifyIssueConsumerRemoved(InitialFilePath);
    }

    private static object[][] ProjectInfos =>
    [
        ["project 2", Guid.NewGuid()],
        [string.Empty, Guid.Empty],
        [null, Guid.Empty]
    ];
    [DynamicData(nameof(ProjectInfos))]
    [DataTestMethod]
    public void UpdateAnalysisState_UpdatedMetadata_CreatesIssueConsumerWithNewSnapshot(string projectName, Guid projectGuid)
    {
        var newTypescriptContentType = Substitute.For<IContentType>();
        var newProjectInfo = (projectName, projectGuid);
        var newTextSnapshot = CreateTextSnapshotMock();
        var issueConsumer = Substitute.For<IIssueConsumer>();
        const string newFilePath = "newName.ts";
        MockGetDocumentProjectInfo(newFilePath, newProjectInfo);
        mockedJavascriptDocumentFooJs.FilePath.Returns(newFilePath);
        mockedJavascriptDocumentFooJs.TextBuffer.ContentType.Returns(newTypescriptContentType);
        mockedJavascriptDocumentFooJs.TextBuffer.CurrentSnapshot.Returns(newTextSnapshot);
        MockIssueConsumerFactory(mockedJavascriptDocumentFooJs, newFilePath, newTextSnapshot, newProjectInfo, issueConsumer);
        RaiseFileRenamedEvent(mockedJavascriptDocumentFooJs, newFilePath); // force metadata update
        ClearMocks();

        var updateAnalysisState = testSubject.UpdateFileState();

        updateAnalysisState.FilePath.Should().Be(newFilePath);
        updateAnalysisState.TextSnapshot.Should().Be(newTextSnapshot);
        VerifyCreateIssueConsumerWasCalled(mockedJavascriptDocumentFooJs, newProjectInfo, issueConsumer, updateAnalysisState);
        VerifyMetadataNotUpdated();
    }

    [TestMethod]
    public void UpdateAnalysisState_CreatesIssueConsumerWithInitialSnapshot()
    {
        var issueConsumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(mockedJavascriptDocumentFooJs, InitialFilePath, initialTextSnapshot, initialProjectInfo, issueConsumer);
        ClearMocks();

        var updateAnalysisState = testSubject.UpdateFileState();

        updateAnalysisState.FilePath.Should().Be(InitialFilePath);
        updateAnalysisState.TextSnapshot.Should().BeSameAs(initialTextSnapshot);
        VerifyCreateIssueConsumerWasCalled(mockedJavascriptDocumentFooJs, initialProjectInfo, issueConsumer, updateAnalysisState);
        VerifyIssueConsumerRemoved(InitialFilePath);
        VerifyMetadataNotUpdated();
    }

    [TestMethod]
    public void UpdateAnalysisState_NonCriticalException_IsSuppressed()
    {
        SetUpIssueConsumerStorageThrows(new InvalidOperationException());

        var act = () => testSubject.UpdateFileState();

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists(string.Format(Strings.Analysis_ErrorUpdatingAnalysisState, string.Empty));
    }

    [TestMethod]
    public void UpdateAnalysisState_CriticalException_IsNotSuppressed()
    {
        SetUpIssueConsumerStorageThrows(new DivideByZeroException("this is a test"));

        var act = () => testSubject.UpdateFileState();

        act.Should().Throw<DivideByZeroException>()
            .WithMessage("this is a test");
    }

    private static readonly List<ITextChange> EmptyAndWhitespaceChanges =
    [
        CreateTextChange("", ""),
        CreateTextChange(" ", ""),
        CreateTextChange("\r\n", "\t\n"),
    ];
    private static readonly List<ITextChange> NonEmptyChanges =
    [
        CreateTextChange("", ""),
        CreateTextChange("\r\n", "\t\nSOMETEXT"),
    ];
    private static object[][] OnTextBufferChangedOnBackground_Updates_Params =>
    [
        [EmptyAndWhitespaceChanges],
        [NonEmptyChanges]
    ];
    [DynamicData(nameof(OnTextBufferChangedOnBackground_Updates_Params))]
    [DataTestMethod]
    public void OnTextBufferChangedOnBackground_Updates(List<ITextChange> changes)
    {
        ClearMocks();
        mockedJavascriptDocumentFooJs.TextBuffer.CurrentSnapshot.Version.Changes.Returns(new TestableNormalizedTextChangeCollection(changes));
        RaiseTextBufferChangedOnBackground(currentTextBuffer: mockDocumentTextBuffer, CreateTextSnapshotMock());

        documentTrackerUpdater.Received(1).OnDocumentUpdated(testSubject);
        VerifyMetadataNotUpdated();
        VerifyIssueConsumerNotCreated();
        VerifyIssueConsumerNotRemoved();
    }

    private static ITextChange CreateTextChange(string before, string after)
    {
        var textChange = Substitute.For<ITextChange>();
        textChange.NewText.Returns(after);
        textChange.OldText.Returns(before);
        return textChange;
    }

    private void SetUpIssueConsumerStorageThrows(Exception exception) => issueConsumerStorage.When(x => x.Remove(Arg.Any<string>())).Do(x => throw exception);

    private static void VerifySingletonManagerDoesNotExist(ITextBuffer buffer) => FindSingletonManagerInPropertyCollection(buffer).Should().BeNull();

    private static void VerifySingletonManagerExists(ITextBuffer buffer) => FindSingletonManagerInPropertyCollection(buffer).Should().NotBeNull();

    private static SingletonDisposableTaggerManager<IErrorTag> FindSingletonManagerInPropertyCollection(ITextBuffer buffer)
    {
        buffer.Properties.TryGetProperty<SingletonDisposableTaggerManager<IErrorTag>>(TaggerProvider.SingletonManagerPropertyCollectionKey,
            out var propertyValue);
        return propertyValue;
    }

    private void CheckFactoryWasRegisteredWithDataSource(IssuesSnapshotFactory factory, int times) => mockSonarErrorDataSource.Received(times).AddFactory(factory);

    private void CheckFactoryWasUnregisteredFromDataSource(IssuesSnapshotFactory factory, int times) => mockSonarErrorDataSource.Received(times).RemoveFactory(factory);

    private static ITextSnapshot CreateTextSnapshotMock(string content = TextContent)
    {
        var textSnapshot = Substitute.For<ITextSnapshot>();
        textSnapshot.GetText().Returns(content);
        return textSnapshot;
    }

    private static ITextBuffer CreateTextBufferMock(ITextSnapshot textSnapshot, IContentType contentType)
    {
        // Text buffer with a properties collection and current snapshot
        var mockTextBuffer = Substitute.For<ITextBuffer2>();

        var dummyProperties = new PropertyCollection();
        mockTextBuffer.Properties.Returns(dummyProperties);
        mockTextBuffer.ContentType.Returns(contentType);
        mockTextBuffer.CurrentSnapshot.Returns(textSnapshot);

        return mockTextBuffer;
    }

    private static ITextDocument CreateDocumentMock(string fileName, ITextBuffer textBufferMock)
    {
        var mockTextDocument = Substitute.For<ITextDocument>();
        mockTextDocument.FilePath.Returns(fileName);
        mockTextDocument.TextBuffer.Returns(textBufferMock);
        mockTextDocument.Encoding.Returns(Encoding.UTF8);

        return mockTextDocument;
    }

    private void MockIssueConsumerFactory(
        ITextDocument document,
        string filePath,
        ITextSnapshot snapshot,
        (string projectName, Guid projectGuid) projectData,
        IIssueConsumer consumerToReturn) =>
        issueConsumerFactory
            .Create(document,
                filePath,
                snapshot,
                projectData.projectName,
                projectData.projectGuid,
                Arg.Any<SnapshotChangedHandler>())
            .Returns(consumerToReturn);

    private void MockGetDocumentProjectInfo(string filePath, (string projectName, Guid projectGuid) projectInfo) => vsProjectInfoProvider.GetDocumentProjectInfo(filePath).Returns(projectInfo);

    private void VerifyMetadataUpdated(string filePath, IContentType contentType, IEnumerable<AnalysisLanguage> detectedLanguages)
    {
        languageRecognizer.Received(1).Detect(filePath, contentType);
        languageRecognizer.ClearReceivedCalls();
        vsProjectInfoProvider.Received(1).GetDocumentProjectInfo(filePath);
        vsProjectInfoProvider.ClearReceivedCalls();
        testSubject.FilePath.Should().Be(filePath);
        testSubject.DetectedLanguages.Should().BeEquivalentTo(detectedLanguages);
    }

    private void VerifyMetadataNotUpdated()
    {
        languageRecognizer.DidNotReceiveWithAnyArgs().Detect(default, default);
        vsProjectInfoProvider.DidNotReceiveWithAnyArgs().GetDocumentProjectInfo(default);
    }

    private void VerifyCreateIssueConsumerWasCalled(
        ITextDocument document,
        (string projectName, Guid projectGuid) projectInfo,
        IIssueConsumer issueConsumerToVerify,
        FileSnapshot FileSnapshot)
    {
        issueConsumerFactory.Received().Create(document, FileSnapshot.FilePath, FileSnapshot.TextSnapshot, projectInfo.projectName, projectInfo.projectGuid, Arg.Any<SnapshotChangedHandler>());
        issueConsumerStorage.Received().Set(FileSnapshot.FilePath, issueConsumerToVerify);
    }

    private void VerifyIssueConsumerNotCreated()
    {
        vsProjectInfoProvider.DidNotReceiveWithAnyArgs().GetDocumentProjectInfo(default);
        issueConsumerFactory.DidNotReceiveWithAnyArgs().Create(default, default, default, default, default, default);
        issueConsumerStorage.DidNotReceiveWithAnyArgs().Set(default, default);
    }

    private void VerifyIssueConsumerNotRemoved() => issueConsumerStorage.DidNotReceiveWithAnyArgs().Remove(default);

    private void VerifyIssueConsumerRemoved(string filePath) => issueConsumerStorage.Received(1).Remove(filePath);

    private void ClearMocks()
    {
        languageRecognizer.ClearReceivedCalls();
        issueConsumerStorage.ClearReceivedCalls();
        issueConsumerFactory.ClearReceivedCalls();
        vsProjectInfoProvider.ClearReceivedCalls();
    }

    private static void RaiseTextBufferChangedOnBackground(ITextBuffer currentTextBuffer, ITextSnapshot newTextSnapshot)
    {
        var args = new TextContentChangedEventArgs(currentTextBuffer.CurrentSnapshot, newTextSnapshot, EditOptions.DefaultMinimalChange, null);
        ((ITextBuffer2)currentTextBuffer).ChangedOnBackground += Raise.EventWith(null, args);
    }

    private static void RaiseFileSavedEvent(ITextDocument mockDocument) => RaiseFileEvent(mockDocument, FileActionTypes.ContentSavedToDisk);

    private static void RaiseFileLoadedEvent(ITextDocument mockDocument) => RaiseFileEvent(mockDocument, FileActionTypes.ContentLoadedFromDisk);

    private static void RaiseFileRenamedEvent(ITextDocument mockDocument, string newFilePath) => RaiseFileEvent(mockDocument, FileActionTypes.DocumentRenamed, newFilePath);

    private static void RaiseFileEvent(ITextDocument textDocument, FileActionTypes actionType, string filePath = null)
    {
        var args = new TextDocumentFileActionEventArgs(filePath ?? textDocument.FilePath, DateTime.UtcNow, actionType);
        textDocument.FileActionOccurred += Raise.EventWith(null, args);
    }
}
