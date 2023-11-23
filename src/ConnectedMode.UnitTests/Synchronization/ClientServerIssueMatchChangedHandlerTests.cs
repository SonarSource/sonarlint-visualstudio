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
using System.Collections.Generic;
using SonarLint.VisualStudio.ConnectedMode.Synchronization;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Synchronization
{
    [TestClass]
    public class ClientServerIssueMatchChangedHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ClientServerIssueMatchChangedHandler, ClientServerIssueMatchChangedHandler>(
                MefTestHelpers.CreateExport<IClientServerIssueSynchronizer>(),
                MefTestHelpers.CreateExport<IIssueLocationStoreAggregator>());
        }

        [TestMethod]
        public void Ctor_SubscribesToEvent()
        {
            var synchronizer = new Mock<IClientServerIssueSynchronizer>();

            _ = CreateTestSubject(synchronizer.Object);

            synchronizer.VerifyAdd(x => x.ClientServerIssueMatchChanged += It.IsAny<EventHandler<ClientServerIssueMatchChangedEventArgs>>(), Times.Once());
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var synchronizer = new Mock<IClientServerIssueSynchronizer>();

            var testSubject = CreateTestSubject(synchronizer.Object);

            testSubject.Dispose();

            synchronizer.VerifyRemove(x => x.ClientServerIssueMatchChanged -= It.IsAny<EventHandler<ClientServerIssueMatchChangedEventArgs>>(), Times.Once());
        }

        [TestMethod]
        public void InvokeEvent_RefreshIsCalled()
        {
            var locationStore = new Mock<IIssueLocationStoreAggregator>();
            var eventArgs = new ClientServerIssueMatchChangedEventArgs(new[] { "file1", "file2" });

            var clientSuppressionSynchronizer = new Mock<IClientServerIssueSynchronizer>();

            var testSubject = new ClientServerIssueMatchChangedHandler(clientSuppressionSynchronizer.Object, locationStore.Object);

            clientSuppressionSynchronizer.Raise(x => x.ClientServerIssueMatchChanged += null, eventArgs);

            locationStore.Verify(x => x.Refresh(It.IsAny<IEnumerable<string>>()), Times.Once);
            locationStore.VerifyNoOtherCalls();

            locationStore.Invocations[0].Arguments[0].Should().BeEquivalentTo(eventArgs.ChangedFiles);
        }

        private static ClientServerIssueMatchChangedHandler CreateTestSubject(IClientServerIssueSynchronizer clientServerIssueSynchronizer = null,
            IIssueLocationStoreAggregator issueLocationStore = null)
        {
            clientServerIssueSynchronizer ??= Mock.Of<IClientServerIssueSynchronizer>();
            issueLocationStore ??= Mock.Of<IIssueLocationStoreAggregator>();
            return new ClientServerIssueMatchChangedHandler(clientServerIssueSynchronizer, issueLocationStore);
        }
    }
}
