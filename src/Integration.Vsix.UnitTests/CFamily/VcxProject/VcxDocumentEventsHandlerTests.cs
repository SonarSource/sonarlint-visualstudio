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
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class VcxDocumentEventsHandlerTests
{
    private VcxDocumentEventsHandler testSubject;
    private IDocumentTracker documentTracker;
    private IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater;
    private const string CFamilyOldFile = "old.cpp";
    private const string CFamilyNewFile = "new.cpp";
    private const string NonCFamilyOldFile = "old.js";
    private const string NonCFamilyNewFile = "new.js";
    private static readonly Document CFamilyDocument = new(CFamilyNewFile, [AnalysisLanguage.CFamily]);
    private static readonly Document NonCFamilyDocument = new(NonCFamilyNewFile, [AnalysisLanguage.Javascript]);

    [TestInitialize]
    public void TestInitialize()
    {
        documentTracker = Substitute.For<IDocumentTracker>();
        vcxCompilationDatabaseUpdater = Substitute.For<IVcxCompilationDatabaseUpdater>();
        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VcxDocumentEventsHandler, IVcxDocumentEventsHandler>(
            MefTestHelpers.CreateExport<IDocumentTracker>(),
            MefTestHelpers.CreateExport<IVcxCompilationDatabaseUpdater>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VcxDocumentEventsHandler>();

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
    public void DocumentOpened_CFamily_AddFileAsyncCalled()
    {
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
    }

    [TestMethod]
    public void DocumentClosed_CFamily_RemoveFileAsyncCalled()
    {
        var args = new DocumentEventArgs(CFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyDocument.FullPath);
    }

    [TestMethod]
    public void OpenDocumentRenamed_CFamily_RemoveAndAddFileAsyncCalled()
    {
        var args = new DocumentRenamedEventArgs(CFamilyDocument, CFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyOldFile);
        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
    }

    [TestMethod]
    public void DocumentOpened_NonCFamily_NoAddFileAsyncCalled()
    {
        var args = new DocumentEventArgs(NonCFamilyDocument);

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().AddFileAsync(Arg.Any<string>());
    }

    [TestMethod]
    public void DocumentClosed_NonCFamily_NoRemoveFileAsyncCalled()
    {
        var args = new DocumentEventArgs(NonCFamilyDocument);

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().RemoveFileAsync(Arg.Any<string>());
    }

    [TestMethod]
    public void DocumentSaved_CFamily_AddFileAsyncCalled()
    {
        var args = new DocumentSavedEventArgs(CFamilyDocument, string.Empty);

        documentTracker.DocumentSaved += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyDocument.FullPath);
    }

    [TestMethod]
    public void OpenDocumentRenamed_NonCFamily_NoRemoveOrAddFileAsyncCalled()
    {
        var args = new DocumentRenamedEventArgs(NonCFamilyDocument, NonCFamilyOldFile);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().RemoveFileAsync(Arg.Any<string>());
        vcxCompilationDatabaseUpdater.DidNotReceive().AddFileAsync(Arg.Any<string>());
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

    private VcxDocumentEventsHandler CreateTestSubject() => new(documentTracker, vcxCompilationDatabaseUpdater);

    private void ClearReceivedCalls()
    {
        documentTracker.ClearReceivedCalls();
        vcxCompilationDatabaseUpdater.ClearReceivedCalls();
    }
}
