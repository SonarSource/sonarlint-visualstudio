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

using SonarLint.VisualStudio.ConnectedMode.Promote;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Promote;

[TestClass]
public class PromoteGoldBarTests
{
    private const string LanguageToPromote = "TSQL";

    private INotificationService notificationService;
    private IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IBrowserService browserService;
    private ITelemetryManager telemetryManager;
    private IConnectedModeUIManager connectedModeUiManager;
    private PromoteGoldBar testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        notificationService = Substitute.For<INotificationService>();
        doNotShowAgainNotificationAction = Substitute.For<IDoNotShowAgainNotificationAction>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        browserService = Substitute.For<IBrowserService>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        connectedModeUiManager = Substitute.For<IConnectedModeUIManager>();
        testSubject = new PromoteGoldBar(notificationService, doNotShowAgainNotificationAction, activeSolutionBoundTracker, browserService, telemetryManager, connectedModeUiManager);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<PromoteGoldBar, IPromoteGoldBar>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IDoNotShowAgainNotificationAction>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<IBrowserService>(),
            MefTestHelpers.CreateExport<ITelemetryManager>(),
            MefTestHelpers.CreateExport<IConnectedModeUIManager>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<PromoteGoldBar>();

    [TestMethod]
    public void PromoteConnectedMode_ShowsNotification_WithId()
    {
        testSubject.PromoteConnectedMode(LanguageToPromote);

        notificationService.Received(1).ShowNotification(Arg.Is<Notification>(n => n.Id == "PromoteNotification"));
    }

    [TestMethod]
    public void PromoteConnectedMode_ShowsNotification_WithMessageThatContainsTheLanguageToPromote()
    {
        testSubject.PromoteConnectedMode(LanguageToPromote);

        notificationService.Received(1).ShowNotification(Arg.Is<Notification>(n =>
            n.Message == string.Format(Resources.PromoteConnectedModeLanguagesMessage, LanguageToPromote)
        ));
    }

    [TestMethod]
    public void PromoteConnectedMode_ShowsNotification_WithCorrectActions()
    {
        testSubject.PromoteConnectedMode(LanguageToPromote);

        notificationService.Received(1).ShowNotification(Arg.Is<Notification>(n =>
            n.Actions.ToList().Count == 4 &&
            n.Actions.ToList()[0].CommandText == Resources.PromoteBind &&
            n.Actions.ToList()[1].CommandText == Resources.PromoteSonarQubeCloud &&
            n.Actions.ToList()[2].CommandText == Resources.PromoteLearnMore &&
            n.Actions.ToList()[3] == doNotShowAgainNotificationAction
        ));
    }

    [TestMethod]
    public void PromoteConnectedMode_BindAction_ShowsManageBindingDialog()
    {
        testSubject.PromoteConnectedMode(LanguageToPromote);
        var notification = (Notification)notificationService.ReceivedCalls().Single().GetArguments()[0];
        var bindAction = notification.Actions.First(a => a.CommandText == Resources.PromoteBind);

        bindAction.Action(null);

        connectedModeUiManager.Received(1).ShowManageBindingDialog();
    }

    [TestMethod]
    public void PromoteConnectedMode_SonarQubeCloudAction_NavigatesToCorrectUrl()
    {
        testSubject.PromoteConnectedMode(LanguageToPromote);
        var notification = (Notification) notificationService.ReceivedCalls().Single().GetArguments()[0];
        var sonarQubeCloudAction = notification.Actions.First(a => a.CommandText == Resources.PromoteSonarQubeCloud);

        sonarQubeCloudAction.Action(null);

        browserService.Received(1).Navigate(TelemetryLinks.LinkIdToUrls[TelemetryLinks.SonarQubeCloudFreeSignUpId]);
    }

    [TestMethod]
    public void PromoteConnectedMode_LearnMoreAction_NavigatesToCorrectUrl()
    {
        testSubject.PromoteConnectedMode(LanguageToPromote);
        var notification = (Notification) notificationService.ReceivedCalls().Single().GetArguments()[0];
        var learnMoreAction = notification.Actions.First(a => a.CommandText == Resources.PromoteLearnMore);

        learnMoreAction.Action(null);

        browserService.Received(1).Navigate(DocumentationLinks.ConnectedModeBenefits);
    }

    [TestMethod]
    public void OnActiveSolutionBindingChanged_ConnectedMode_ClosesNotification()
    {
        var eventArgs = new ActiveSolutionBindingEventArgs(new BindingConfiguration(null, SonarLintMode.Connected, null));

        testSubject.PromoteConnectedMode(LanguageToPromote);
        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(this, eventArgs);

        notificationService.Received(1).CloseNotification();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromAllEvents()
    {
        testSubject.Dispose();

        activeSolutionBoundTracker.Received(1).SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
    }
}
