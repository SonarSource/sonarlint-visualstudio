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

using System.Collections.Generic;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots;

[TestClass]
public class HotspotSolutionClosedHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
        =>  MefTestHelpers.CheckTypeCanBeImported<HotspotSolutionClosedHandler, IHotspotSolutionClosedHandler>(
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<ILocalHotspotsStoreUpdater>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void CheckIsSharedMefComponent()
        => MefTestHelpers.CheckIsSingletonMefComponent<HotspotSolutionClosedHandler>();

    [DataRow(SonarLintMode.Connected, false)]
    [DataRow(SonarLintMode.LegacyConnected, false)]
    [DataRow(SonarLintMode.Standalone, true)]
    [DataTestMethod]
    public void SolutionEventRaised_ClearsDependingOnMode(SonarLintMode mode, bool shouldBeCleared)
    {
        var localHotspotStoreUpdaterMock = new Mock<ILocalHotspotsStoreUpdater>();
        var activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>();

        CreateTestSubject(localHotspotStoreUpdaterMock.Object, activeSolutionBoundTrackerMock.Object);

        RaiseSolutionEvent(activeSolutionBoundTrackerMock, mode);
        
        localHotspotStoreUpdaterMock.Verify(x => x.Clear(), shouldBeCleared ? Times.Once : Times.Never);
    }
    
    [DataTestMethod]
    public void SolutionEventRaised_HandledOnBackgroundThread()
    {
        var callSequence = new List<string>();

        var localHotspotStoreUpdaterMock = new Mock<ILocalHotspotsStoreUpdater>();
        var activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>();
        var threadHandlingMock = new Mock<IThreadHandling>();
        
        localHotspotStoreUpdaterMock.Setup(x => x.Clear())
            .Callback(() => callSequence.Add("Clear"));

        threadHandlingMock.Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> action) =>
            {
                callSequence.Add("RunOnBackgroundThread");
                return action();
            });
        
        localHotspotStoreUpdaterMock.Invocations.Should().BeEmpty();

        CreateTestSubject(localHotspotStoreUpdaterMock.Object, activeSolutionBoundTrackerMock.Object, threadHandlingMock.Object);

        RaiseSolutionEvent(activeSolutionBoundTrackerMock, SonarLintMode.Standalone);
        
        callSequence.Should().ContainInOrder("RunOnBackgroundThread", "Clear");
    }
    
    [TestMethod]
    public void Dispose_EventHandlerIsUnregistered()
    {
        var activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>();

        var testSubject = CreateTestSubject(activeSolutionBoundTracker: activeSolutionBoundTrackerMock.Object);
        activeSolutionBoundTrackerMock
            .VerifyAdd(x => x.SolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);

        testSubject.Dispose();
        activeSolutionBoundTrackerMock
            .VerifyRemove(x => x.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
    }
    
    private static void RaiseSolutionEvent(Mock<IActiveSolutionBoundTracker> activeSolutionBoundTrackerMock, SonarLintMode mode)
        => activeSolutionBoundTrackerMock
            .Raise(x => x.SolutionBindingChanged += null,
                new ActiveSolutionBindingEventArgs(new BindingConfiguration(null, mode, null)));

    private static HotspotSolutionClosedHandler CreateTestSubject(ILocalHotspotsStoreUpdater localHotspotsStoreUpdater = null,
        IActiveSolutionBoundTracker activeSolutionBoundTracker = null,
        IThreadHandling threadHandling = null)
    {
        return new HotspotSolutionClosedHandler(localHotspotsStoreUpdater ?? Mock.Of<ILocalHotspotsStoreUpdater>(),
            activeSolutionBoundTracker ?? Mock.Of<IActiveSolutionBoundTracker>(),
            threadHandling ?? new NoOpThreadHandler());
    }
}
