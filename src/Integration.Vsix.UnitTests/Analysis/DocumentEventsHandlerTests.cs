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

using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.File;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class DocumentEventsHandlerTests
{
    private IDocumentTracker documentTracker;
    private IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private ILogger logger;
    private IFileRpcSLCoreService fileRpcSlCoreService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IThreadHandling threadHandling;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private readonly IRequireInitialization[] initializationDependencies = [];
    private const string CFamilyOldFile = "file:///tmp/SLVS/old.cpp";
    private const string CFamilyNewFile = "file:///tmp/SLVS/new.cpp";
    private const string NonCFamilyOldFile = "file:///tmp/SLVS/old.js";
    private const string NonCFamilyNewFile = "file:///tmp/SLVS/new.js";
    private static readonly Document CFamilyDocument = new(CFamilyNewFile, [AnalysisLanguage.CFamily]);
    private static readonly Document CFamilyDocument2 = new(CFamilyOldFile, [AnalysisLanguage.CFamily]);
    private static readonly Document NonCFamilyDocument = new(NonCFamilyNewFile, [AnalysisLanguage.Javascript]);
    private static readonly Document NonCFamilyDocument2 = new(NonCFamilyOldFile, [AnalysisLanguage.Javascript]);
    private static readonly ConfigurationScope ConfigurationScope = new("test-scope-id", RootPath: "D:\\");

    [TestInitialize]
    public void TestInitialize()
    {
        documentTracker = Substitute.For<IDocumentTracker>();
        documentTracker.GetOpenDocuments().Returns(new List<Document>());
        vcxCompilationDatabaseUpdater = Substitute.For<IVcxCompilationDatabaseUpdater>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        threadHandling = Substitute.For<IThreadHandling>();
        MockThreadHandling();
        MockCurrentConfigScope(ConfigurationScope);
        MockSlCoreServices();
        logger = Substitute.For<ILogger>();
        logger.ForVerboseContext(Arg.Any<string[]>()).Returns(logger);
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<DocumentEventsHandler>(threadHandling, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DocumentEventsHandler, IDocumentEventsHandler>(
            MefTestHelpers.CreateExport<IDocumentTracker>(),
            MefTestHelpers.CreateExport<IVcxCompilationDatabaseUpdater>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>()
        );

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<DocumentEventsHandler>();

    [TestMethod]
    public void Ctor_InitializesInCorrectOrder()
    {
        var testSubject = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            logger.ForVerboseContext(nameof(DocumentEventsHandler));
            initializationProcessorFactory.Create<DocumentEventsHandler>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(initializationDependencies)), Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync();
            activeConfigScopeTracker.CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
            documentTracker.DocumentOpened += Arg.Any<EventHandler<DocumentOpenedEventArgs>>();
            documentTracker.DocumentClosed += Arg.Any<EventHandler<DocumentEventArgs>>();
            documentTracker.DocumentSaved += Arg.Any<EventHandler<DocumentSavedEventArgs>>();
            documentTracker.OpenDocumentRenamed += Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
            documentTracker.GetOpenDocuments(); // the remaining logic is tested in other tests
            slCoreServiceProvider.TryGetTransientService(out Arg.Any<IFileRpcSLCoreService>());
            testSubject.InitializationProcessor.InitializeAsync(); // called by CreateAndInitializeTestSubject
        });
    }

    [TestMethod]
    public void Ctor_WithOpenCFamilyFiles_AddsToCompilationDbAndNotifiesSlCore()
    {
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument, NonCFamilyDocument, CFamilyDocument2, NonCFamilyDocument2]);
        var testSubject = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            testSubject.InitializationProcessor.InitializeAsync();
            documentTracker.GetOpenDocuments();
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument.FullPath);
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument2.FullPath);
            fileRpcSlCoreService.DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument.FullPath)));
            fileRpcSlCoreService.DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument.FullPath)));
            fileRpcSlCoreService.DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument2.FullPath)));
            fileRpcSlCoreService.DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument2.FullPath)));
            testSubject.InitializationProcessor.InitializeAsync(); // called by CreateAndInitializeTestSubject
        });
    }

    [TestMethod]
    public void Ctor_NoConfigurationScope_WithOpenCFamilyFiles_AddsToCompilationDb_DoesNotNotifySlCore()
    {
        MockCurrentConfigScope(null);
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument, NonCFamilyDocument, CFamilyDocument2, NonCFamilyDocument2]);
        var testSubject = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            testSubject.InitializationProcessor.InitializeAsync();
            documentTracker.GetOpenDocuments();
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument.FullPath);
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument2.FullPath);
            testSubject.InitializationProcessor.InitializeAsync(); // called by CreateAndInitializeTestSubject
        });

        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(default);
        logger.Received(1).LogVerbose(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void Ctor_SlCoreServiceNotAvailable_WithOpenCFamilyFiles_AddsToCompilationDb_DoesNotNotifySlCore()
    {
        MockFileRpcService(service: null, succeeds: false);
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument, NonCFamilyDocument, CFamilyDocument2, NonCFamilyDocument2]);
        var testSubject = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            testSubject.InitializationProcessor.InitializeAsync();
            documentTracker.GetOpenDocuments();
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument.FullPath);
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument2.FullPath);
            testSubject.InitializationProcessor.InitializeAsync(); // called by CreateAndInitializeTestSubject
        });

        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(default);
        logger.Received(1).LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void DocumentOpened_CFamily_AddFileToCompilationDbAndNotifiesSlCore()
    {
        CreateAndInitializeTestSubject();
        var args = new DocumentOpenedEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentClosed_CFamily_RemoveFileFromCompilationDbAndNotifiesSlCore()
    {
        CreateAndInitializeTestSubject();
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.Received(1)
            .DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void OpenDocumentRenamed_CFamily_RenamesFileInCompilationDbAndNotifiesSlCore()
    {
        CreateAndInitializeTestSubject();
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
        fileRpcSlCoreService.Received(1).DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, CFamilyOldFile)));
        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentOpened_NonCFamily_DoesNotAddFileToCompilationDbButNotifiesSlCore()
    {
        CreateAndInitializeTestSubject();
        var args = new DocumentOpenedEventArgs(NonCFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().AddFileAsync(Arg.Any<string>());
        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentClosed_NonCFamily_DoesNotRemoveFileFromCompilationDbButNotifiesSlCore()
    {
        CreateAndInitializeTestSubject();
        var args = new DocumentEventArgs(NonCFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().RemoveFileAsync(Arg.Any<string>());
        fileRpcSlCoreService.Received(1)
            .DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentSaved_CFamily_AddFileToCompilationDb()
    {
        CreateAndInitializeTestSubject();
        var args = new DocumentSavedEventArgs(CFamilyDocument);

        documentTracker.DocumentSaved += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
    }

    [TestMethod]
    public void OpenDocumentRenamed_NonCFamily_DoesNotRenameFileFromCompilationDbButNotifiesSlCore()
    {
        CreateAndInitializeTestSubject();
        var args = new DocumentRenamedEventArgs(NonCFamilyDocument, NonCFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceiveWithAnyArgs().RenameFileAsync(default, default);
        fileRpcSlCoreService.Received(1)
            .DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, NonCFamilyOldFile)));
        fileRpcSlCoreService.Received(1).DidOpenFile(Arg.Is<DidOpenFileParams>(x =>
            x.configurationScopeId == ConfigurationScope.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentSaved_NonCFamily_DoesNothing()
    {
        var args = new DocumentSavedEventArgs(NonCFamilyDocument);

        documentTracker.DocumentSaved += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.ReceivedCalls().Should().HaveCount(0);
        fileRpcSlCoreService.ReceivedCalls().Should().HaveCount(0);
    }

    [TestMethod]
    public void DocumentOpened_ExecutesOnBackgroundThread()
    {
        CreateAndInitializeTestSubject();
        threadHandling.ClearReceivedCalls();
        var args = new DocumentOpenedEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument.FullPath);
            _ = activeConfigScopeTracker.Current;
            fileRpcSlCoreService.DidOpenFile(Arg.Any<DidOpenFileParams>());
        });
    }

    [TestMethod]
    public void DocumentClosed_ExecutesOnBackgroundThread()
    {
        CreateAndInitializeTestSubject();
        threadHandling.ClearReceivedCalls();
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            vcxCompilationDatabaseUpdater.RemoveFileAsync(CFamilyDocument.FullPath);
            _ = activeConfigScopeTracker.Current;
            fileRpcSlCoreService.DidCloseFile(Arg.Any<DidCloseFileParams>());
        });
    }

    [TestMethod]
    public void OpenDocumentRenamed_ExecutesOnBackgroundThread()
    {
        CreateAndInitializeTestSubject();
        threadHandling.ClearReceivedCalls();
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            vcxCompilationDatabaseUpdater.RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
            _ = activeConfigScopeTracker.Current;
            fileRpcSlCoreService.DidCloseFile(Arg.Any<DidCloseFileParams>());
            fileRpcSlCoreService.DidOpenFile(Arg.Any<DidOpenFileParams>());
        });
    }

    [TestMethod]
    public void DocumentSaved_ExecutesOnBackgroundThread()
    {
        CreateAndInitializeTestSubject();
        threadHandling.ClearReceivedCalls();
        var args = new DocumentSavedEventArgs(CFamilyDocument);

        documentTracker.DocumentSaved += Raise.EventWith(documentTracker, args);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument.FullPath);
        });
    }

    [TestMethod]
    public void DocumentOpened_SlCoreServiceNotAvailable_DoesNotNotifySlCoreAndLogs()
    {
        CreateAndInitializeTestSubject();
        MockFileRpcService(service: null, succeeds: false);
        var args = new DocumentOpenedEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(1).LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void DocumentClosed_SlCoreServiceNotAvailable_DoesNotNotifySlCoreAndLogs()
    {
        CreateAndInitializeTestSubject();
        MockFileRpcService(service: null, succeeds: false);
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        logger.Received(1).LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void OpenDocumentRenamed_SlCoreServiceNotAvailable_DoesNotNotifySlCoreAndLogs()
    {
        CreateAndInitializeTestSubject();
        MockFileRpcService(service: null, succeeds: false);
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(2).LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void DocumentOpened_CurrentConfigScopeIsNull_DoesNotNotifySlCoreAndLogs()
    {
        CreateAndInitializeTestSubject();
        MockCurrentConfigScope(configurationScope: null);
        var args = new DocumentOpenedEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(1).LogVerbose(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void DocumentClosed_CurrentConfigScopeIsNull_DoesNotNotifySlCoreAndLogs()
    {
        CreateAndInitializeTestSubject();
        MockCurrentConfigScope(configurationScope: null);
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        logger.Received(1).LogVerbose(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void OpenDocumentRenamed_CurrentConfigScopeIsNull_DoesNotNotifySlCoreAndLogs()
    {
        CreateAndInitializeTestSubject();
        MockCurrentConfigScope(configurationScope: null);
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(2).LogVerbose(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void Dispose_BeforeInitialization_DoesNotUnsubscribe()
    {
        var subject = CreateUninitializedTestSubject(out var barrier);

        subject.Dispose();
        barrier.SetResult(1);
        subject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        activeConfigScopeTracker.DidNotReceive().CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        activeConfigScopeTracker.DidNotReceive().CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        documentTracker.DidNotReceive().DocumentOpened += Arg.Any<EventHandler<DocumentOpenedEventArgs>>();
        documentTracker.DidNotReceive().DocumentOpened -= Arg.Any<EventHandler<DocumentOpenedEventArgs>>();
        documentTracker.DidNotReceive().DocumentClosed += Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.DidNotReceive().DocumentClosed -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.DidNotReceive().DocumentSaved += Arg.Any<EventHandler<DocumentSavedEventArgs>>();
        documentTracker.DidNotReceive().DocumentSaved -= Arg.Any<EventHandler<DocumentSavedEventArgs>>();
        documentTracker.DidNotReceive().OpenDocumentRenamed += Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
        documentTracker.DidNotReceive().OpenDocumentRenamed -= Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }

    [TestMethod]
    public void Dispose_AfterInitialization_Unsubscribes()
    {
        var subject = CreateAndInitializeTestSubject();

        subject.Dispose();
        subject.Dispose();
        subject.Dispose();

        activeConfigScopeTracker.Received(1).CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        documentTracker.Received(1).DocumentClosed -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received(1).DocumentOpened -= Arg.Any<EventHandler<DocumentOpenedEventArgs>>();
        documentTracker.Received(1).DocumentSaved -= Arg.Any<EventHandler<DocumentSavedEventArgs>>();
        documentTracker.Received(1).OpenDocumentRenamed -= Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }

    [TestMethod]
    public void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged_DefinitionChanged_TriggersNotification()
    {
        CreateAndInitializeTestSubject();
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument, NonCFamilyDocument]);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(new ConfigurationScopeChangedEventArgs(definitionChanged: true));

        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && x.fileUri.LocalPath == new FileUri(CFamilyDocument.FullPath).LocalPath));
        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == ConfigurationScope.Id && x.fileUri.LocalPath == new FileUri(NonCFamilyDocument.FullPath).LocalPath));
    }

    [TestMethod]
    public void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged_DefinitionNotChanged_DoesNothing()
    {
        CreateAndInitializeTestSubject();
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument, NonCFamilyDocument]);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(new ConfigurationScopeChangedEventArgs(definitionChanged: false));

        fileRpcSlCoreService.DidNotReceive().DidOpenFile(Arg.Any<DidOpenFileParams>());
    }

    [TestMethod]
    public void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged_NoActiveConfigurationScope_DoesNothing()
    {
        MockCurrentConfigScope(null);
        CreateAndInitializeTestSubject();
        logger.ClearReceivedCalls();
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument, NonCFamilyDocument]);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(new ConfigurationScopeChangedEventArgs(definitionChanged: true));

        fileRpcSlCoreService.DidNotReceive().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(1).LogVerbose(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged_SlCoreServiceNotAvailable_DoesNothing()
    {
        MockFileRpcService(service: null, succeeds: false);
        CreateAndInitializeTestSubject();
        logger.ClearReceivedCalls();
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument, NonCFamilyDocument]);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(new ConfigurationScopeChangedEventArgs(definitionChanged: true));

        fileRpcSlCoreService.DidNotReceive().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(1).LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
    }

    private DocumentEventsHandler CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<DocumentEventsHandler>(threadHandling, logger, processor =>
        {
            MockableInitializationProcessor.ConfigureWithWait(processor, tcs);
        });
        return new DocumentEventsHandler(
            documentTracker,
            vcxCompilationDatabaseUpdater,
            slCoreServiceProvider,
            activeConfigScopeTracker,
            initializationProcessorFactory,
            threadHandling,
            logger);
    }

    private DocumentEventsHandler CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<DocumentEventsHandler>(threadHandling, logger);
        var handler = new DocumentEventsHandler(
            documentTracker,
            vcxCompilationDatabaseUpdater,
            slCoreServiceProvider,
            activeConfigScopeTracker,
            initializationProcessorFactory,
            threadHandling,
            logger);
        handler.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return handler;
    }

    private void MockSlCoreServices()
    {
        fileRpcSlCoreService = Substitute.For<IFileRpcSLCoreService>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        MockFileRpcService(fileRpcSlCoreService, succeeds: true);
    }

    private void MockFileRpcService(IFileRpcSLCoreService service, bool succeeds) =>
        slCoreServiceProvider.TryGetTransientService(out IFileRpcSLCoreService _).Returns(x =>
        {
            x[0] = service;
            return succeeds;
        });

    private void MockThreadHandling() =>
        threadHandling.When(x => x.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>())).Do(x =>
        {
            var func = x.Arg<Func<Task<int>>>();
            func();
        });

    private static bool IsExpectedFileUri(FileUri fileUri, string path) => fileUri.LocalPath == new FileUri(path).LocalPath;

    private void MockCurrentConfigScope(ConfigurationScope configurationScope) => activeConfigScopeTracker.Current.Returns(configurationScope);
}
