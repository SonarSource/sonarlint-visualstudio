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

using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding;

[TestClass]
public class BindingSuggestionHandlerTests
{
    private BindingSuggestionHandler testSubject;
    private INotificationService notificationService;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IIDEWindowService ideWindowService;
    private IConnectedModeUIManager connectedModeManager;
    private IBrowserService browserService;

    [TestInitialize]
    public void TestInitialize()
    {
        notificationService = Substitute.For<INotificationService>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        ideWindowService = Substitute.For<IIDEWindowService>();
        connectedModeManager = Substitute.For<IConnectedModeUIManager>();
        browserService = Substitute.For<IBrowserService>();
        testSubject = new BindingSuggestionHandler(notificationService, activeSolutionBoundTracker, ideWindowService, connectedModeManager, browserService);
    }

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
        MockCurrentConfiguration(sonarLintMode);

        testSubject.Notify();

        ideWindowService.Received().BringToFront();
    }

    [TestMethod]
    public void Notify_WithStandaloneProject_PromptsToConnect()
    {
        MockCurrentConfiguration(SonarLintMode.Standalone);

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
        MockCurrentConfiguration(SonarLintMode.Connected);

        testSubject.Notify();

        notificationService.Received().ShowNotification(Arg.Is<INotification>(
            n => n.Message.Equals(BindingStrings.BindingSuggetsionBindingConflict)
                 && n.Actions
                     .Select(x => x.CommandText)
                     .SequenceEqual(new[] { BindingStrings.BindingSuggestionLearnMore })));
    }

    [TestMethod]
    public void Notify_ConnectAction_ShowsManageBindingDialog()
    {
        MockCurrentConfiguration(SonarLintMode.Standalone);

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
        MockCurrentConfiguration(SonarLintMode.Standalone);

        testSubject.Notify();

        var notification = (Notification)notificationService.ReceivedCalls().Single().GetArguments().Single();
        var connectAction = notification.Actions.First(x => x.CommandText.Equals(BindingStrings.BindingSuggestionLearnMore));

        browserService.DidNotReceiveWithAnyArgs().Navigate(default);
        connectAction.Action(notification);

        browserService.Received().Navigate(DocumentationLinks.OpenInIdeBindingSetup);
    }
    
    [TestMethod]
    [DataRow(SonarLintMode.Connected, true)]
    [DataRow(SonarLintMode.Standalone, false)]
    public void SolutionBindingChanged_WhenConnectedMode_ClosesAnyOpenGoldBar(SonarLintMode mode, bool expectedToClose)
    {
        RaiseSolutionBindingChanged(mode);

        notificationService.Received(expectedToClose ? 1 : 0).CloseNotification();
    }
    
    [TestMethod]
    public void Dispose_UnsubscribesFromAllEvents()
    {
        testSubject.Dispose();

        activeSolutionBoundTracker.Received(1).SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
    }

    private void MockCurrentConfiguration(SonarLintMode sonarLintMode)
    {
        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(
            new BoundServerProject("solution", "server project", new ServerConnection.SonarCloud("org")), sonarLintMode,
            "a-directory"));
    }
    
    private void RaiseSolutionBindingChanged(SonarLintMode mode)
    {
        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(new BindingConfiguration(null, mode, string.Empty)));
    }
}
