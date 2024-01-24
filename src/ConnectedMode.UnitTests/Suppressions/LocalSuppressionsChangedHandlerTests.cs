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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class LocalSuppressionsChangedHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<LocalSuppressionsChangedHandler, LocalSuppressionsChangedHandler>(
                MefTestHelpers.CreateExport<IClientSuppressionSynchronizer>(),
                MefTestHelpers.CreateExport<IIssueLocationStoreAggregator>());
        }

        [TestMethod]
        public void Ctor_SubscribesToEvent()
        {
            var synchronizer = new Mock<IClientSuppressionSynchronizer>();

            _ = CreateTestSubject(synchronizer.Object);

            synchronizer.VerifyAdd(x => x.LocalSuppressionsChanged += It.IsAny<EventHandler<LocalSuppressionsChangedEventArgs>>(), Times.Once());
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var synchronizer = new Mock<IClientSuppressionSynchronizer>();

            var testSubject = CreateTestSubject(synchronizer.Object);

            testSubject.Dispose();

            synchronizer.VerifyRemove(x => x.LocalSuppressionsChanged -= It.IsAny<EventHandler<LocalSuppressionsChangedEventArgs>>(), Times.Once());
        }

        [TestMethod]
        public void InvokeEvent_RefreshIsCalled()
        {
            var locationStore = new Mock<IIssueLocationStoreAggregator>();
            var eventArgs = new LocalSuppressionsChangedEventArgs(new[] { "file1", "file2" });

            var clientSuppressionSynchronizer = new Mock<IClientSuppressionSynchronizer>();

            var testSubject = new LocalSuppressionsChangedHandler(clientSuppressionSynchronizer.Object, locationStore.Object);

            clientSuppressionSynchronizer.Raise(x => x.LocalSuppressionsChanged += null, eventArgs);

            locationStore.Verify(x => x.Refresh(It.IsAny<IEnumerable<string>>()), Times.Once);
            locationStore.VerifyNoOtherCalls();

            locationStore.Invocations[0].Arguments[0].Should().BeEquivalentTo(eventArgs.ChangedFiles);
        }

        private static LocalSuppressionsChangedHandler CreateTestSubject(IClientSuppressionSynchronizer clientSuppressionSynchronizer = null,
            IIssueLocationStoreAggregator issueLocationStore = null)
        {
            clientSuppressionSynchronizer ??= Mock.Of<IClientSuppressionSynchronizer>();
            issueLocationStore ??= Mock.Of<IIssueLocationStoreAggregator>();
            return new LocalSuppressionsChangedHandler(clientSuppressionSynchronizer, issueLocationStore);
        }
    }
}
