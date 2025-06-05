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
    private IDocumentEvents documentEvents;
    private IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater;

    private const string CFamilyFile = "file.cpp";
    private const string CFamilyOldFile = "old.cpp";
    private const string CFamilyNewFile = "new.cpp";
    private const string NonCFamilyFile = "file.js";
    private const string NonCFamilyOldFile = "old.js";
    private const string NonCFamilyNewFile = "new.js";
    private static readonly AnalysisLanguage[] CFamilyLanguage = [AnalysisLanguage.CFamily];
    private static readonly AnalysisLanguage[] NonCFamilyLanguage = [AnalysisLanguage.Javascript];

    [TestInitialize]
    public void TestInitialize()
    {
        documentEvents = Substitute.For<IDocumentEvents>();
        vcxCompilationDatabaseUpdater = Substitute.For<IVcxCompilationDatabaseUpdater>();
        testSubject = new VcxDocumentEventsHandler(documentEvents, vcxCompilationDatabaseUpdater);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VcxDocumentEventsHandler, IVcxDocumentEventsHandler>(
            MefTestHelpers.CreateExport<IDocumentEvents>(),
            MefTestHelpers.CreateExport<IVcxCompilationDatabaseUpdater>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
            MefTestHelpers.CheckIsSingletonMefComponent<VcxDocumentEventsHandler>();



    [TestMethod]
    public void Ctor_SubscribesToAllDocumentEvents()
    {
        documentEvents.Received(1).DocumentClosed += Arg.Any<EventHandler<DocumentClosedEventArgs>>();
        documentEvents.Received(1).DocumentOpened += Arg.Any<EventHandler<DocumentOpenedEventArgs>>();
        documentEvents.Received(1).OpenDocumentRenamed += Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }

    [TestMethod]
    public void DocumentOpened_CFamily_AddFileAsyncCalled()
    {
        var args = new DocumentOpenedEventArgs(CFamilyFile, CFamilyLanguage);

        documentEvents.DocumentOpened += Raise.EventWith(documentEvents, args);

        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyFile);
    }

    [TestMethod]
    public void DocumentClosed_CFamily_RemoveFileAsyncCalled()
    {
        var args = new DocumentClosedEventArgs(CFamilyFile, CFamilyLanguage);

        documentEvents.DocumentClosed += Raise.EventWith(documentEvents, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyFile);
    }

    [TestMethod]
    public void OpenDocumentRenamed_CFamily_RemoveAndAddFileAsyncCalled()
    {
        var args = new DocumentRenamedEventArgs(CFamilyNewFile, CFamilyOldFile, CFamilyLanguage);

        documentEvents.OpenDocumentRenamed += Raise.EventWith(documentEvents, args);

        vcxCompilationDatabaseUpdater.Received(1).RemoveFileAsync(CFamilyOldFile);
        vcxCompilationDatabaseUpdater.Received(1).AddFileAsync(CFamilyNewFile);
    }

    [TestMethod]
    public void DocumentOpened_NonCFamily_NoAddFileAsyncCalled()
    {
        var args = new DocumentOpenedEventArgs(NonCFamilyFile, NonCFamilyLanguage);

        documentEvents.DocumentOpened += Raise.EventWith(documentEvents, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().AddFileAsync(Arg.Any<string>());
    }

    [TestMethod]
    public void DocumentClosed_NonCFamily_NoRemoveFileAsyncCalled()
    {
        var args = new DocumentClosedEventArgs(NonCFamilyFile, NonCFamilyLanguage);

        documentEvents.DocumentClosed += Raise.EventWith(documentEvents, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().RemoveFileAsync(Arg.Any<string>());
    }

    [TestMethod]
    public void OpenDocumentRenamed_NonCFamily_NoRemoveOrAddFileAsyncCalled()
    {
        var args = new DocumentRenamedEventArgs(NonCFamilyOldFile, NonCFamilyNewFile, NonCFamilyLanguage);

        documentEvents.OpenDocumentRenamed += Raise.EventWith(documentEvents, args);

        vcxCompilationDatabaseUpdater.DidNotReceive().RemoveFileAsync(Arg.Any<string>());
        vcxCompilationDatabaseUpdater.DidNotReceive().AddFileAsync(Arg.Any<string>());
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromAllDocumentEvents()
    {
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        documentEvents.Received(1).DocumentClosed -= Arg.Any<EventHandler<DocumentClosedEventArgs>>();
        documentEvents.Received(1).DocumentOpened -= Arg.Any<EventHandler<DocumentOpenedEventArgs>>();
        documentEvents.Received(1).OpenDocumentRenamed -= Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }
}
