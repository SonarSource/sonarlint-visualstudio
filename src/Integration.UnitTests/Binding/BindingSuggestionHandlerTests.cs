﻿/*
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

using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding;

[TestClass]
public class BindingSuggestionHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<BindingSuggestionHandler, IBindingSuggestionHandler>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<IIDEWindowService>(),
            MefTestHelpers.CreateExport<IConnectedModeUIManager>(),
            MefTestHelpers.CreateExport<IBrowserService>());
    }

    [TestMethod]
    [DataRow(SonarLintMode.Standalone)]
    [DataRow(SonarLintMode.Connected)]
    public void Notify_BringsWindowToFront(SonarLintMode sonarLintMode)
    {
        var ideWindowService = Substitute.For<IIDEWindowService>();

        var testSubject = CreateTestSubject(sonarLintMode: sonarLintMode, ideWindowService: ideWindowService);
        testSubject.Notify();

        ideWindowService.Received().BringToFront();
    }

    [TestMethod]
    public void Notify_WithStandaloneProject_PromptsToConnect()
    {
        var notificationService = Substitute.For<INotificationService>();

        var testSubject = CreateTestSubject(sonarLintMode: SonarLintMode.Standalone, notificationService: notificationService);
        testSubject.Notify();

        notificationService.Received().ShowNotification(Arg.Is<INotification>(
            n => n.Message.Equals(BindingStrings.BindingSuggestionProjectNotBound)
                 && n.Actions
                     .Select(x => x.CommandText)
                     .SequenceEqual(new []{ BindingStrings.BindingSuggestionConnect, BindingStrings.BindingSuggestionLearnMore })));
    }

    [TestMethod]
    public void Notify_WithBoundProject_ShowsConflictMessage()
    {
        var notificationService = Substitute.For<INotificationService>();

        var testSubject = CreateTestSubject(sonarLintMode: SonarLintMode.Connected, notificationService: notificationService);
        testSubject.Notify();

        notificationService.Received().ShowNotification(Arg.Is<INotification>(
            n => n.Message.Equals(BindingStrings.BindingSuggetsionBindingConflict)
                 && n.Actions
                     .Select(x => x.CommandText)
                     .SequenceEqual(new[] { BindingStrings.BindingSuggestionLearnMore })));
    }

    [TestMethod]
    public void Notify_ConnectAction_OpensSonarQubePage()
    {
        var notificationService = Substitute.For<INotificationService>();
        var connectedModeManager = Substitute.For<IConnectedModeUIManager>();

        var testSubject = CreateTestSubject(sonarLintMode: SonarLintMode.Standalone, notificationService: notificationService, connectedModeUiManager: connectedModeManager);
        testSubject.Notify();
        var notification = (Notification)notificationService.ReceivedCalls().Single().GetArguments().Single();
        var connectAction = notification.Actions.First(x => x.CommandText.Equals(BindingStrings.BindingSuggestionConnect));

        connectedModeManager.DidNotReceive().ShowManageBindingDialog();
        connectAction.Action(notification);

        connectedModeManager.Received().ShowManageBindingDialog();
    }

    [TestMethod]
    public void Notify_LearnMoreAction_OpensDocumentationInBrowser()
    {
        var notificationService = Substitute.For<INotificationService>();
        var browserService = Substitute.For<IBrowserService>();

        var testSubject = CreateTestSubject(sonarLintMode: SonarLintMode.Standalone, notificationService: notificationService, browserService: browserService);
        testSubject.Notify();
        var notification = (Notification)notificationService.ReceivedCalls().Single().GetArguments().Single();
        var connectAction = notification.Actions.First(x => x.CommandText.Equals(BindingStrings.BindingSuggestionLearnMore));

        browserService.DidNotReceiveWithAnyArgs().Navigate(default);
        connectAction.Action(notification);

        browserService.Received().Navigate(DocumentationLinks.OpenInIdeBindingSetup);
    }

    private BindingSuggestionHandler CreateTestSubject(SonarLintMode sonarLintMode,
        INotificationService notificationService = null,
        IIDEWindowService ideWindowService = null,
        IConnectedModeUIManager connectedModeUiManager = null,
        IBrowserService browserService = null)
    {
        notificationService ??= Substitute.For<INotificationService>();
        var activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        ideWindowService ??= Substitute.For<IIDEWindowService>();
        connectedModeUiManager ??= Substitute.For<IConnectedModeUIManager>();
        browserService ??= Substitute.For<IBrowserService>();

        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(new BoundServerProject("solution", "server project", new ServerConnection.SonarCloud("org")), sonarLintMode, "a-directory"));

        return new BindingSuggestionHandler(notificationService, activeSolutionBoundTracker, ideWindowService, connectedModeUiManager, browserService);
    }
}
