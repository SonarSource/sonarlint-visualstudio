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

using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.SupportedLanguages;

[TestClass]
public class FailedPluginNotificationTests
{
    private IPluginStatusesStore pluginStatusesStore = null!;
    private INotificationService notificationService = null!;
    private ISLCoreHandler slCoreHandler = null!;
    private ISupportedLanguagesWindowService supportedLanguagesWindowService = null!;
    private FailedPluginNotification testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        pluginStatusesStore = Substitute.For<IPluginStatusesStore>();
        notificationService = Substitute.For<INotificationService>();
        slCoreHandler = Substitute.For<ISLCoreHandler>();
        supportedLanguagesWindowService = Substitute.For<ISupportedLanguagesWindowService>();

        testSubject = new FailedPluginNotification(pluginStatusesStore, notificationService, slCoreHandler, supportedLanguagesWindowService);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<FailedPluginNotification, IFailedPluginNotification>(
            MefTestHelpers.CreateExport<IPluginStatusesStore>(),
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<ISLCoreHandler>(),
            MefTestHelpers.CreateExport<ISupportedLanguagesWindowService>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FailedPluginNotification>();

    [TestMethod]
    public void PluginStatusesChanged_NoFailedPlugins_DoesNotShowNotification()
    {
        pluginStatusesStore.GetAll().Returns([
            new PluginStatusDisplay("Java", PluginStateDto.ACTIVE, null, string.Empty),
            new PluginStatusDisplay("C#", PluginStateDto.ACTIVE, null, string.Empty)
        ]);

        RaisePluginStatusesChanged();

        notificationService.DidNotReceive().ShowNotification(Arg.Any<INotification>());
        notificationService.Received(1).CloseNotification();
    }

    [TestMethod]
    public void PluginStatusesChanged_HasFailedPlugins_ShowsNotificationWithCorrectMessage()
    {
        pluginStatusesStore.GetAll().Returns([
            new PluginStatusDisplay("Java", PluginStateDto.FAILED, null, string.Empty),
            new PluginStatusDisplay("C#", PluginStateDto.ACTIVE, null, string.Empty),
            new PluginStatusDisplay("C++", PluginStateDto.FAILED, null, string.Empty)
        ]);

        RaisePluginStatusesChanged();

        notificationService.Received(1).ShowNotification(Arg.Is<Notification>(n =>
            n.Message.StartsWith(Strings.PluginStatusesFailedNotificationText) && n.Message.EndsWith("Java, C++")
            && n.Id == "PluginStatuses.FailedNotification"
            && n.ShowOncePerSession == false));
    }

    [TestMethod]
    public void PluginStatusesChanged_HasFailedPlugins_NotificationHasTwoActions()
    {
        SetupFailedPlugins();

        RaisePluginStatusesChanged();

        GetNotification().Actions.Should().HaveCount(2);
    }

    [TestMethod]
    public void PluginStatusesChanged_HasFailedPlugins_RestartActionRestartsBackend()
    {
        SetupFailedPlugins();

        RaisePluginStatusesChanged();

        var notification = GetNotification();
        var restartAction = notification.Actions.ToList()[0];

        restartAction.CommandText.Should().Be(SLCoreStrings.SloopRestartFailedNotificationService_Restart);
        restartAction.ShouldDismissAfterAction.Should().BeTrue();
        restartAction.Action(notification);
        slCoreHandler.Received(1).ForceRestartSloop();
    }

    [TestMethod]
    public void PluginStatusesChanged_HasFailedPlugins_SeeDetailsActionOpensWindow()
    {
        SetupFailedPlugins();

        RaisePluginStatusesChanged();

        var notification = GetNotification();
        var seeDetailsAction = notification.Actions.ToList()[1];

        seeDetailsAction.CommandText.Should().Be(Strings.PluginStatusesFailedNotificationSeeDetailsButton);
        seeDetailsAction.ShouldDismissAfterAction.Should().BeTrue();
        seeDetailsAction.Action(notification);
        supportedLanguagesWindowService.Received(1).Show();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvent()
    {
        testSubject.Dispose();

        pluginStatusesStore.Received(1).PluginStatusesChanged -= Arg.Any<EventHandler>();
    }

    private void SetupFailedPlugins()
    {
        pluginStatusesStore.GetAll().Returns([
            new PluginStatusDisplay("Java", PluginStateDto.FAILED, null, string.Empty)
        ]);
    }

    private Notification GetNotification()
    {
        return (Notification)notificationService.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(INotificationService.ShowNotification))
            .GetArguments()[0]!;
    }

    private void RaisePluginStatusesChanged()
    {
        pluginStatusesStore.PluginStatusesChanged += Raise.EventWith(EventArgs.Empty);
    }
}
