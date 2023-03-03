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
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.TestInfrastructure;

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
                MefTestHelpers.CreateExport<ISuppressedIssuesMonitor>());
        }

        [TestMethod]
        public void Ctor_SubscribesToEvent()
        {
            var suppressedIssuesMonitor = new Mock<ISuppressedIssuesMonitor>();

            _ = new ServerSuppressionsChangedHandler(Mock.Of<IClientSuppressionSynchronizer>(), suppressedIssuesMonitor.Object);

            suppressedIssuesMonitor.VerifyAdd(x => x.ServerSuppressionsChanged += It.IsAny<EventHandler>(), Times.Once());
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var suppressedIssuesMonitor = new Mock<ISuppressedIssuesMonitor>();

            var testSubject = new ServerSuppressionsChangedHandler(Mock.Of<IClientSuppressionSynchronizer>(), suppressedIssuesMonitor.Object);

            testSubject.Dispose();

            suppressedIssuesMonitor.VerifyRemove(x => x.ServerSuppressionsChanged -= It.IsAny<EventHandler>(), Times.Once());
        }

        [TestMethod]
        public void InvokeEvent_SynchronizeSuppressedIssuesIsCalled()
        {
            var suppressedIssuesMonitor = new Mock<ISuppressedIssuesMonitor>();
            var clientSuppressionSynchronizer = new Mock<IClientSuppressionSynchronizer>();

            _ = new ServerSuppressionsChangedHandler(clientSuppressionSynchronizer.Object, suppressedIssuesMonitor.Object);

            suppressedIssuesMonitor.Raise(x => x.ServerSuppressionsChanged += null, EventArgs.Empty);

            clientSuppressionSynchronizer.Verify(x => x.SynchronizeSuppressedIssues(), Times.Once);
        }
    }
}
