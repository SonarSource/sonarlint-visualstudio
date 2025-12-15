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

using SonarLint.VisualStudio.Core.SmartNotification;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.SmartNotification;

[TestClass]
public class SmartNotificationServiceTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SmartNotificationService, ISmartNotificationService>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SmartNotificationService>();

    [TestMethod]
    public void ShowSmartNotification_InvokesNotificationReceivedEvent_WithCorrectNotification()
    {
        var service = new SmartNotificationService();
        Core.SmartNotification.SmartNotification receivedNotification = null;
        var expectedNotification = new Core.SmartNotification.SmartNotification("A notification", "http://localhost:9000/project/overview", ["SCOPE_ID1"], "CATEGORY", "http://localhost:9000", DateTimeOffset.Now);
        service.NotificationReceived += (_, args) => { receivedNotification = args.Notification; };

        service.ShowSmartNotification(expectedNotification);

        receivedNotification.Should().NotBeNull("the NotificationReceived event should have been invoked.");
        receivedNotification.Should().BeEquivalentTo(expectedNotification, options => options.ComparingByMembers<Core.SmartNotification.SmartNotification>());
    }
}
