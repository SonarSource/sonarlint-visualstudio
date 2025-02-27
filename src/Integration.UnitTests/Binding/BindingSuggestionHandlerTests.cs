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

using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding;

[TestClass]
public class BindingSuggestionHandlerTests
{
    private const string ProjectKey = "projectKey";
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
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<BindingSuggestionHandler, IBindingSuggestionHandler>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<IIDEWindowService>(),
            MefTestHelpers.CreateExport<IConnectedModeUIManager>(),
            MefTestHelpers.CreateExport<IBrowserService>());

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Notify_BringsWindowToFront(bool isSonarCloud)
    {
        testSubject.Notify(ProjectKey, isSonarCloud);

        ideWindowService.Received().BringToFront();
    }

    [TestMethod]
    public void Notify_SonarCloud_ShowsMessageAndActions()
    {
        var expectedMessage = string.Format(BindingStrings.NoBindingMatch, UiResources.SonarQubeCloud, ProjectKey);

        testSubject.Notify(ProjectKey, isSonarCloud: true);

        notificationService.Received().ShowNotification(Arg.Is<INotification>(
            n => n.Message.Equals(expectedMessage)
                 && n.Actions
                     .Select(x => x.CommandText)
                     .SequenceEqual(new[] { BindingStrings.ConfigureBinding, BindingStrings.BindingSuggestionLearnMore })));
    }

    [TestMethod]
    public void Notify_SonarQube_ShowsMessageAndActions()
    {
        var expectedMessage = string.Format(BindingStrings.NoBindingMatch, UiResources.SonarQubeServer, ProjectKey);

        testSubject.Notify(ProjectKey, isSonarCloud: false);

        notificationService.Received().ShowNotification(Arg.Is<INotification>(
            n => n.Message.Equals(expectedMessage)
                 && n.Actions
                     .Select(x => x.CommandText)
                     .SequenceEqual(new[] { BindingStrings.ConfigureBinding, BindingStrings.BindingSuggestionLearnMore })));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Notify_ConnectAction_ShowsManageBindingDialog(bool isSonarCloud)
    {
        testSubject.Notify(ProjectKey, isSonarCloud);
        var notification = GetNotification();
        var connectAction = GetAction(notification, BindingStrings.ConfigureBinding);
        connectedModeManager.DidNotReceive().ShowManageBindingDialog();

        connectAction.Action(notification);

        connectedModeManager.Received().ShowManageBindingDialog();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Notify_LearnMoreAction_OpensDocumentationInBrowser(bool isSonarCloud)
    {
        testSubject.Notify(ProjectKey, isSonarCloud);
        var notification = GetNotification();
        var connectAction = GetAction(notification, BindingStrings.BindingSuggestionLearnMore);
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

    private void RaiseSolutionBindingChanged(SonarLintMode mode) =>
        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(new BindingConfiguration(null, mode, string.Empty)));

    private static INotificationAction GetAction(Notification notification, string commandText) => notification.Actions.First(x => x.CommandText.Equals(commandText));

    private Notification GetNotification() => (Notification)notificationService.ReceivedCalls().Single().GetArguments().Single();
}
