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
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class BoundSolutionUpdateHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BoundSolutionUpdateHandler, BoundSolutionUpdateHandler>(
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<ISuppressionIssueStoreUpdater>(),
                MefTestHelpers.CreateExport<IServerHotspotStoreUpdater>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<BoundSolutionUpdateHandler>();
        }

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            _ = new BoundSolutionUpdateHandler(activeSolutionTracker.Object, Mock.Of<ISuppressionIssueStoreUpdater>(), Mock.Of<IServerHotspotStoreUpdater>());

            activeSolutionTracker.VerifyAdd(x => x.SolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyAdd(x => x.SolutionBindingUpdated += It.IsAny<EventHandler>(), Times.Once);
        }

        [TestMethod]
        public void InvokeEvents_ServerStoreUpdatersAreCalled()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();
            var suppressionIssueStoreUpdater = new Mock<ISuppressionIssueStoreUpdater>();
            var serverHotspotStoreUpdater = new Mock<IServerHotspotStoreUpdater>();

            _ = new BoundSolutionUpdateHandler(activeSolutionTracker.Object, suppressionIssueStoreUpdater.Object, serverHotspotStoreUpdater.Object);

            activeSolutionTracker.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));
            suppressionIssueStoreUpdater.Verify(x => x.UpdateAllServerSuppressionsAsync(), Times.Once);
            serverHotspotStoreUpdater.Verify(x => x.UpdateAllServerHotspotsAsync(), Times.Once);

            activeSolutionTracker.Raise(x => x.SolutionBindingUpdated += null, EventArgs.Empty);
            suppressionIssueStoreUpdater.Verify(x => x.UpdateAllServerSuppressionsAsync(), Times.Exactly(2));
            serverHotspotStoreUpdater.Verify(x => x.UpdateAllServerHotspotsAsync(), Times.Exactly(2));
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            var testSubject = new BoundSolutionUpdateHandler(activeSolutionTracker.Object, Mock.Of<ISuppressionIssueStoreUpdater>(), Mock.Of<IServerHotspotStoreUpdater>());

            testSubject.Dispose();

            activeSolutionTracker.VerifyRemove(x => x.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyRemove(x => x.SolutionBindingUpdated -= It.IsAny<EventHandler>(), Times.Once);
        }
    }
}
