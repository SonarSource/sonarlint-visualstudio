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
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.File;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class DocumentEventsHandlerTests
{
    private DocumentEventsHandler testSubject;
    private IDocumentTracker documentTracker;
    private IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private ILogger logger;
    private IFileRpcSLCoreService fileRpcSlCoreService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IThreadHandling threadHandling;
    private const string CFamilyOldFile = "file:///tmp/SLVS/old.cpp";
    private const string CFamilyNewFile = "file:///tmp/SLVS/new.cpp";
    private const string NonCFamilyOldFile = "file:///tmp/SLVS/old.js";
    private const string NonCFamilyNewFile = "file:///tmp/SLVS/new.js";
    private static readonly Document CFamilyDocument = new(CFamilyNewFile, [AnalysisLanguage.CFamily]);
    private static readonly Document NonCFamilyDocument = new(NonCFamilyNewFile, [AnalysisLanguage.Javascript]);
    private static readonly ConfigurationScope ConfigurationScope = new("test-scope-id", RootPath: "D:\\");

    [TestInitialize]
    public void TestInitialize()
    {
        documentTracker = Substitute.For<IDocumentTracker>();
        vcxCompilationDatabaseUpdater = Substitute.For<IVcxCompilationDatabaseUpdater>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        threadHandling = Substitute.For<IThreadHandling>();
        MockThreadHandling();
        MockCurrentConfigScope(ConfigurationScope);
        MockSlCoreServices();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DocumentEventsHandler, IDocumentEventsHandler>(
            MefTestHelpers.CreateExport<IDocumentTracker>(),
            MefTestHelpers.CreateExport<IVcxCompilationDatabaseUpdater>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>()
        );

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<DocumentEventsHandler>();

    [TestMethod]
    public void Ctor_AddsAlreadyOpenedFilesToDb()
    {
        ClearReceivedCalls();
        documentTracker.GetOpenDocuments().Returns([CFamilyDocument]);

        CreateTestSubject();

        Received.InOrder(() =>
        {
            documentTracker.DocumentOpened += Arg.Any<EventHandler<DocumentEventArgs>>();
            documentTracker.DocumentClosed += Arg.Any<EventHandler<DocumentEventArgs>>();
            documentTracker.DocumentSaved += Arg.Any<EventHandler<DocumentSavedEventArgs>>();
            documentTracker.OpenDocumentRenamed += Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
            vcxCompilationDatabaseUpdater.AddFileAsync(CFamilyDocument.FullPath);
        });
    }

    [TestMethod]
    public void Ctor_SubscribesToAllDocumentEvents()
    {
        documentTracker.Received(1).DocumentClosed += Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received(1).DocumentOpened += Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received(1).DocumentSaved += Arg.Any<EventHandler<DocumentSavedEventArgs>>();
        documentTracker.Received(1).OpenDocumentRenamed += Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }

    [TestMethod]
    public void Ctor_SetsContext() => logger.Received(1).ForContext(nameof(DocumentEventsHandler));

    [TestMethod]
    public void DocumentOpened_CFamily_AddFileToCompilationDbAndNotifiesSlCore()
    {
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentClosed_CFamily_RemoveFileFromCompilationDbAndNotifiesSlCore()
    {
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.Received(1)
            .DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void OpenDocumentRenamed_CFamily_RenamesFileInCompilationDbAndNotifiesSlCore()
    {
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
        fileRpcSlCoreService.Received(1).DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, CFamilyOldFile)));
        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, CFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentOpened_NonCFamily_DoesNotAddFileToCompilationDbButNotifiesSlCore()
    {
        var args = new DocumentEventArgs(NonCFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().AddFileAsync(Arg.Any<string>());
        fileRpcSlCoreService.Received(1)
            .DidOpenFile(Arg.Is<DidOpenFileParams>(x => x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentClosed_NonCFamily_DoesNotRemoveFileFromCompilationDbButNotifiesSlCore()
    {
        var args = new DocumentEventArgs(NonCFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().RemoveFileAsync(Arg.Any<string>());
        fileRpcSlCoreService.Received(1)
            .DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentSaved_CFamily_AddFileAsyncCalled()
    {
        var args = new DocumentSavedEventArgs(CFamilyDocument, string.Empty);

        documentTracker.DocumentSaved += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
    }

    [TestMethod]
    public void OpenDocumentRenamed_NonCFamily_DoesNotRenameFileFromCompilationDbButNotifiesSlCore()
    {
        var args = new DocumentRenamedEventArgs(NonCFamilyDocument, NonCFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceiveWithAnyArgs().RenameFileAsync(default, default);
        fileRpcSlCoreService.Received(1)
            .DidCloseFile(Arg.Is<DidCloseFileParams>(x => x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, NonCFamilyOldFile)));
        fileRpcSlCoreService.Received(1).DidOpenFile(Arg.Is<DidOpenFileParams>(x =>
            x.configurationScopeId == activeConfigScopeTracker.Current.Id && IsExpectedFileUri(x.fileUri, NonCFamilyDocument.FullPath)));
    }

    [TestMethod]
    public void DocumentOpened_ExecutesOnBackgroundThread()
    {
        var args = new DocumentEventArgs(CFamilyDocument);

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
    public void DocumentOpened_SlCoreServiceNotAvailable_DoesNotNotifySlCoreAndLogs()
    {
        MockFileRpcService(service: null, succeeds: false);
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(1).WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void DocumentClosed_SlCoreServiceNotAvailable_DoesNotNotifySlCoreAndLogs()
    {
        MockFileRpcService(service: null, succeeds: false);
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        logger.Received(1).WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void OpenDocumentRenamed_SlCoreServiceNotAvailable_DoesNotNotifySlCoreAndLogs()
    {
        MockFileRpcService(service: null, succeeds: false);
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(2).WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void DocumentOpened_CurrentConfigScopeIsNull_DoesNotNotifySlCoreAndLogs()
    {
        MockCurrentConfigScope(configurationScope: null);
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(1).WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void DocumentClosed_CurrentConfigScopeIsNull_DoesNotNotifySlCoreAndLogs()
    {
        MockCurrentConfigScope(configurationScope: null);
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        logger.Received(1).WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void OpenDocumentRenamed_CurrentConfigScopeIsNull_DoesNotNotifySlCoreAndLogs()
    {
        MockCurrentConfigScope(configurationScope: null);
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(2).WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void DocumentOpened_CurrentConfigScopeIdRootNull_DoesNotNotifySlCoreAndLogs()
    {
        MockCurrentConfigScope(ConfigurationScope with { RootPath = null });
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(1).WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void DocumentClosed_CurrentConfigScopeIdRootNull_DoesNotNotifySlCoreAndLogs()
    {
        MockCurrentConfigScope(ConfigurationScope with { RootPath = null });
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        logger.Received(1).WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void OpenDocumentRenamed_CurrentConfigScopeRootIsNull_DoesNotNotifySlCoreAndLogs()
    {
        MockCurrentConfigScope(ConfigurationScope with { RootPath = null });
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RenameFileAsync(CFamilyOldFile, CFamilyDocument.FullPath);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidCloseFile(Arg.Any<DidCloseFileParams>());
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidOpenFile(Arg.Any<DidOpenFileParams>());
        logger.Received(2).WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromAllDocumentEvents()
    {
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        documentTracker.Received(1).DocumentClosed -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received(1).DocumentOpened -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received(1).DocumentSaved -= Arg.Any<EventHandler<DocumentSavedEventArgs>>();
        documentTracker.Received(1).OpenDocumentRenamed -= Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }

    private DocumentEventsHandler CreateTestSubject() => new(documentTracker, vcxCompilationDatabaseUpdater, slCoreServiceProvider, activeConfigScopeTracker, threadHandling, logger);

    private void ClearReceivedCalls()
    {
        documentTracker.ClearReceivedCalls();
        vcxCompilationDatabaseUpdater.ClearReceivedCalls();
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
