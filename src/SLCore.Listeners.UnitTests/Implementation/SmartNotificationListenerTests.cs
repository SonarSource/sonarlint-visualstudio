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
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.SmartNotification;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class SmartNotificationListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SmartNotificationListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ISmartNotificationService>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ShowInIdeListener>();

    [TestMethod]
    public void ShowSmartNotification_NotifiesSmartNotificationServiceWithCorrectParams()
    {
        var smartNotificationService = Substitute.For<ISmartNotificationService>();
        var testSubject = new SmartNotificationListener(smartNotificationService);
        var slCoreSmartNotificationParams
            = new ShowSmartNotificationParams("A notification", "http://localhost:9000/project/overview", ["SCOPE_ID1"], "CATEGORY", "http://localhost:9000");

        testSubject.ShowSmartNotification(slCoreSmartNotificationParams);

        smartNotificationService.Received(1)
            .ShowSmartNotification(Arg.Is<SmartNotification>(actual =>
                actual.Text == slCoreSmartNotificationParams.text &&
                actual.Link == slCoreSmartNotificationParams.link &&
                actual.Category == slCoreSmartNotificationParams.category &&
                actual.ConnectionId == slCoreSmartNotificationParams.connectionId &&
                actual.ScopeIds.SetEquals(slCoreSmartNotificationParams.scopeIds) &&
                actual.Date <= DateTimeOffset.Now
            ));
    }
}
