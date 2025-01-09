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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Http;

[TestClass]
public class ServerCertificateInvalidNotificationTests
{
    private IBrowserService browserService;
    private INotificationService notificationService;
    private IOutputWindowService outputWindowService;
    private ServerCertificateInvalidNotification testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        notificationService = Substitute.For<INotificationService>();
        browserService = Substitute.For<IBrowserService>();
        outputWindowService = Substitute.For<IOutputWindowService>();

        testSubject = new ServerCertificateInvalidNotification(
            notificationService,
            outputWindowService,
            browserService);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ServerCertificateInvalidNotification, IServerCertificateInvalidNotification>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IOutputWindowService>(),
            MefTestHelpers.CreateExport<IBrowserService>()
        );

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ServerCertificateInvalidNotification>();

    [TestMethod]
    public void Show_ExpectedNotification()
    {
        testSubject.Show();

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(x => IsExpectedNotification(x)));
    }

    private bool IsExpectedNotification(INotification x)
    {
        VerifyNotificationHasExpectedActions(x);

        return x.Id == ServerCertificateInvalidNotification.ServerCertificateInvalidNotificationId && x.Message == SLCoreStrings.ServerCertificateInfobar_CertificateInvalidMessage;
    }

    private void VerifyNotificationHasExpectedActions(INotification notification)
    {
        notification.Actions.Should().HaveCount(2);
        notification.Actions.Should().Contain(x => IsExpectedAction(x, SLCoreStrings.ServerCertificateInfobar_LearnMore));
        notification.Actions.Should().Contain(x => IsExpectedAction(x, SLCoreStrings.ServerCertificateInfobar_ShowLogs));

        notification.Actions.First().Action.Invoke(null);
        notification.Actions.Last().Action.Invoke(null);
        browserService.Received(1).Navigate(DocumentationLinks.SslCertificate);
        outputWindowService.Received(1).Show();
    }

    private static bool IsExpectedAction(INotificationAction notificationAction, string expectedText) => notificationAction.CommandText == expectedText && !notificationAction.ShouldDismissAfterAction;
}
