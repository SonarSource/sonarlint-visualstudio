/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using SonarLint.VisualStudio.ConnectedMode.Synchronization;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Synchronization
{
    [TestClass]
    public class ServerIssuesChangedHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerIssuesChangedHandler, ServerIssuesChangedHandler>(
                MefTestHelpers.CreateExport<IClientServerIssueSynchronizer>(),
                MefTestHelpers.CreateExport<IServerIssuesStore>());
        }

        [TestMethod]
        public void Ctor_SubscribesToEvent()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();

            _ = new ServerIssuesChangedHandler(Mock.Of<IClientServerIssueSynchronizer>(), serverIssuesStore.Object);

            serverIssuesStore.VerifyAdd(x => x.ServerIssuesChanged += It.IsAny<EventHandler>(), Times.Once());
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();

            var testSubject = new ServerIssuesChangedHandler(Mock.Of<IClientServerIssueSynchronizer>(), serverIssuesStore.Object);

            testSubject.Dispose();

            serverIssuesStore.VerifyRemove(x => x.ServerIssuesChanged -= It.IsAny<EventHandler>(), Times.Once());
        }

        [TestMethod]
        public void InvokeEvent_SynchronizeSuppressedIssuesIsCalled()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();
            var clientSuppressionSynchronizer = new Mock<IClientServerIssueSynchronizer>();

            _ = new ServerIssuesChangedHandler(clientSuppressionSynchronizer.Object, serverIssuesStore.Object);

            serverIssuesStore.Raise(x => x.ServerIssuesChanged += null, EventArgs.Empty);

            clientSuppressionSynchronizer.Verify(x => x.SynchronizeIssues(), Times.Once);
        }
    }
}
