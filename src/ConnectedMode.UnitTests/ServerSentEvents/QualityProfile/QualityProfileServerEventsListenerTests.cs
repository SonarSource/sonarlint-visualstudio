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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.QualityProfile;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents.QualityProfile;

[TestClass]
public class QualityProfileServerEventsListenerTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<QualityProfileServerEventsListener, IQualityProfileServerEventsListener>(
            MefTestHelpers.CreateExport<IQualityProfileServerEventSource>(),
            MefTestHelpers.CreateExport<IQualityProfileUpdater>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<QualityProfileServerEventsListener>();
    }

    [TestMethod]
    public void ListenAsync_CallsUpdaterForEachEventUntilNullEvent()
    {
        var events = new[]
        {
            Mock.Of<IQualityProfileEvent>(),
            Mock.Of<IQualityProfileEvent>(),
            Mock.Of<IQualityProfileEvent>(),
            null
        };
        var eventWaiters = Enumerable.Range(0, events.Length).Select(_ => new TaskCompletionSource<IQualityProfileEvent>()).ToArray();
        var eventSourceMock = SetUpEventSourceSequence(eventWaiters);
        var threadHandlingMock = SetUpThreadHandlingSwitchToBackgroundThread();
        var updaterMock = new Mock<IQualityProfileUpdater>();

        var testSubject = new QualityProfileServerEventsListener(eventSourceMock.Object, updaterMock.Object, threadHandlingMock.Object);

        var longRunningTask = testSubject.ListenAsync();
        
        for (var index = 0; index < eventWaiters.Length; index++)
        {
            eventWaiters[index].SetResult(events[index]);
            (longRunningTask.Status == TaskStatus.RanToCompletion).Should().Be(events[index] == null); // only completes on null event
        } 
        eventSourceMock.Verify(x => x.GetNextEventOrNullAsync(), Times.Exactly(events.Length));
        updaterMock.Verify(x => x.UpdateAsync(), Times.Exactly(events.Length - 1));
        threadHandlingMock.Verify(th => th.SwitchToBackgroundThread(), Times.Once);
    }

    private static Mock<IThreadHandling> SetUpThreadHandlingSwitchToBackgroundThread()
    {
        var threadHandlingMock = new Mock<IThreadHandling>();
        threadHandlingMock.Setup(th => th.SwitchToBackgroundThread()).Returns(new NoOpThreadHandler.NoOpAwaitable());
        return threadHandlingMock;
    }

    private static Mock<IQualityProfileServerEventSource> SetUpEventSourceSequence(TaskCompletionSource<IQualityProfileEvent>[] eventWaiters)
    {
        var currentEventIndex = 0;
        var eventSourceMock = new Mock<IQualityProfileServerEventSource>();
        eventSourceMock.Setup(x => x.GetNextEventOrNullAsync()).Returns(() => eventWaiters[currentEventIndex++].Task);
        return eventSourceMock;
    }
}
