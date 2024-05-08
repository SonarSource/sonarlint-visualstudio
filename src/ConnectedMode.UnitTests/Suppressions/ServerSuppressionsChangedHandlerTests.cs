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

using SonarLint.VisualStudio.ConnectedMode.Suppressions;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class ServerSuppressionsChangedHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerSuppressionsChangedHandler, ServerSuppressionsChangedHandler>(
                MefTestHelpers.CreateExport<IClientSuppressionSynchronizer>(),
                MefTestHelpers.CreateExport<IServerIssuesStore>());
        }

        [TestMethod]
        public void Ctor_SubscribesToEvent()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();

            _ = new ServerSuppressionsChangedHandler(Mock.Of<IClientSuppressionSynchronizer>(), serverIssuesStore.Object);

            serverIssuesStore.VerifyAdd(x => x.ServerIssuesChanged += It.IsAny<EventHandler>(), Times.Once());
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();

            var testSubject = new ServerSuppressionsChangedHandler(Mock.Of<IClientSuppressionSynchronizer>(), serverIssuesStore.Object);

            testSubject.Dispose();

            serverIssuesStore.VerifyRemove(x => x.ServerIssuesChanged -= It.IsAny<EventHandler>(), Times.Once());
        }

        [TestMethod]
        public void InvokeEvent_SynchronizeSuppressedIssuesIsCalled()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();
            var clientSuppressionSynchronizer = new Mock<IClientSuppressionSynchronizer>();

            _ = new ServerSuppressionsChangedHandler(clientSuppressionSynchronizer.Object, serverIssuesStore.Object);

            serverIssuesStore.Raise(x => x.ServerIssuesChanged += null, EventArgs.Empty);

            clientSuppressionSynchronizer.Verify(x => x.SynchronizeSuppressedIssues(), Times.Once);
        }
    }
}
