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

using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.QualityProfile;
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
    public async Task ListenAsync_CallsUpdaterForEachEventUntilNullEvent()
    {
        var events = new[]
        {
            Task.FromResult(Mock.Of<IQualityProfileEvent>()),
            Task.FromResult(Mock.Of<IQualityProfileEvent>()),
            Task.FromResult(Mock.Of<IQualityProfileEvent>()),
            Task.FromResult<IQualityProfileEvent>(null), // should stop on null no matter what
            Task.FromResult(Mock.Of<IQualityProfileEvent>()),
        };
        var eventSourceMock = SetUpEventSourceSequence(events);
        var threadHandlingMock = SetUpThreadHandlingSwitchToBackgroundThread();
        var updaterMock = new Mock<IQualityProfileUpdater>();

        var testSubject = new QualityProfileServerEventsListener(eventSourceMock.Object, updaterMock.Object, threadHandlingMock.Object);

        await testSubject.ListenAsync();
        
        eventSourceMock.Verify(x => x.GetNextEventOrNullAsync(), Times.Exactly(events.Length - 1 /* the event after null is ignored */));
        updaterMock.Verify(x => x.UpdateAsync(), Times.Exactly(events.Length - 1/* null doesn't trigger an update */ - 1/* the event after null is ignored */));
        threadHandlingMock.Verify(th => th.SwitchToBackgroundThread(), Times.Once);
    }

    private static Mock<IThreadHandling> SetUpThreadHandlingSwitchToBackgroundThread()
    {
        var threadHandlingMock = new Mock<IThreadHandling>();
        threadHandlingMock.Setup(th => th.SwitchToBackgroundThread()).Returns(new NoOpThreadHandler.NoOpAwaitable());
        return threadHandlingMock;
    }

    private static Mock<IQualityProfileServerEventSource> SetUpEventSourceSequence(Task<IQualityProfileEvent>[] events)
    {
        var currentEventIndex = 0;
        var eventSourceMock = new Mock<IQualityProfileServerEventSource>();
        eventSourceMock.Setup(x => x.GetNextEventOrNullAsync()).Returns(() => events[currentEventIndex++]);
        return eventSourceMock;
    }
}
