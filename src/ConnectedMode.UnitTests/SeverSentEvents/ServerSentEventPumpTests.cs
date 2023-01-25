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
using System.Linq;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.SeverSentEvents
{
    [TestClass]
    public class ServerSentEventPumpTests
    {
        private Mock<IServerSentEventsFilter> serverSentEventsFilterMock;
        private Mock<IServerSentEventsSession> serverSentEventsSessionMock;
        private Mock<IIssueChangedServerEventSourcePublisher> issueChangedPublisherMock;
        private Mock<ITaintServerEventSourcePublisher> taintPublisherMock;
        private ServerSentEventPump testSubject;

        [TestInitialize]
        public void SetUp()
        {
            serverSentEventsFilterMock = new Mock<IServerSentEventsFilter>();
            serverSentEventsSessionMock = new Mock<IServerSentEventsSession>();
            issueChangedPublisherMock = new Mock<IIssueChangedServerEventSourcePublisher>();
            taintPublisherMock = new Mock<ITaintServerEventSourcePublisher>();

            testSubject = new ServerSentEventPump(serverSentEventsFilterMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerSentEventPump, IServerSentEventPump>(
                MefTestHelpers.CreateExport<IServerSentEventsFilter>());
        }

        [TestMethod]
        public async Task PumpAllAsync_SelectsPublisherCorrectlyAndPreservesOrderWithinType()
        {
            var inputSequence = new IServerEvent[]
            {
                Mock.Of<IIssueChangedServerEvent>(),
                Mock.Of<ITaintVulnerabilityRaisedServerEvent>(),
                Mock.Of<IIssueChangedServerEvent>(),
                Mock.Of<ITaintVulnerabilityClosedServerEvent>(),
                Mock.Of<ITaintVulnerabilityClosedServerEvent>(),
                Mock.Of<IIssueChangedServerEvent>(),
                Mock.Of<ITaintVulnerabilityRaisedServerEvent>(),
            };
            SetUpEventsSequenceThatFinishes(inputSequence);
            SetUpStubFilter();

            await testSubject.PumpAllAsync(serverSentEventsSessionMock.Object, issueChangedPublisherMock.Object, taintPublisherMock.Object);
            
            issueChangedPublisherMock.Invocations
                .Select(call => call.Arguments.First() as IIssueChangedServerEvent)
                .Should()
                .BeEquivalentTo(inputSequence.Where(issuesEvent => issuesEvent is IIssueChangedServerEvent));
            taintPublisherMock.Invocations
                .Select(call => call.Arguments.First() as ITaintServerEvent)
                .Should()
                .BeEquivalentTo(inputSequence.Where(taintEvent => taintEvent is ITaintServerEvent));
        }

        [TestMethod]
        public async Task PumpAllAsync_RespectsFilter()
        {
            var filteredEvent = Mock.Of<IIssueChangedServerEvent>();
            SetUpEventsSequenceThatFinishes(Mock.Of<IIssueChangedServerEvent>(), filteredEvent, Mock.Of<IIssueChangedServerEvent>());
            serverSentEventsFilterMock.Setup(filter => filter.GetFilteredEventOrNull(It.IsAny<IServerEvent>()))
                .Returns((IServerEvent arg1) => arg1 == filteredEvent ? filteredEvent : null);

            await testSubject.PumpAllAsync(serverSentEventsSessionMock.Object, issueChangedPublisherMock.Object, taintPublisherMock.Object);

            issueChangedPublisherMock.Invocations.Single().Arguments.First().Should().BeSameAs(filteredEvent);
        }

        [TestMethod]
        public async Task PumpAllAsync_WhenPublisherDisposed_Finishes()
        {
            SetUpEventsSequenceThatFinishes(Mock.Of<IIssueChangedServerEvent>(), Mock.Of<IIssueChangedServerEvent>());
            SetUpStubFilter();
            issueChangedPublisherMock.Setup(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()))
                .Throws(new ObjectDisposedException(string.Empty));

            await testSubject.PumpAllAsync(serverSentEventsSessionMock.Object, issueChangedPublisherMock.Object, taintPublisherMock.Object);

            issueChangedPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()), Times.Once);
            taintPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<ITaintServerEvent>()), Times.Never);
        }

        private void SetUpEventsSequenceThatFinishes(params IServerEvent[] inputSequence)
        {
            var sequenceSetUp = serverSentEventsSessionMock.SetupSequence(x => x.ReadAsync());
            sequenceSetUp = inputSequence.Aggregate(sequenceSetUp, (current, serverEvent) => current.ReturnsAsync(serverEvent));
            sequenceSetUp.Throws<OperationCanceledException>();
        }

        private void SetUpStubFilter()
        {
            serverSentEventsFilterMock.Setup(filter => filter.GetFilteredEventOrNull(It.IsAny<IServerEvent>()))
                .Returns((IServerEvent arg1) => arg1);
        }
    }
}
