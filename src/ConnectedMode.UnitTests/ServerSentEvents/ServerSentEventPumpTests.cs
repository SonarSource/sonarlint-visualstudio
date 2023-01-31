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

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents
{
    [TestClass]
    public class ServerSentEventPumpTests
    {
        private Mock<IServerSentEventsSession> serverSentEventsSessionMock;
        private Mock<IIssueChangedServerEventSourcePublisher> issueChangedPublisherMock;
        private Mock<ITaintServerEventSourcePublisher> taintPublisherMock;
        private ServerSentEventPump testSubject;

        [TestInitialize]
        public void SetUp()
        {
            serverSentEventsSessionMock = new Mock<IServerSentEventsSession>();
            issueChangedPublisherMock = new Mock<IIssueChangedServerEventSourcePublisher>();
            taintPublisherMock = new Mock<ITaintServerEventSourcePublisher>();

            testSubject = new ServerSentEventPump(issueChangedPublisherMock.Object, taintPublisherMock.Object);
        }

        [TestMethod]
        public void Dispose_DisposesPublishers()
        {
            testSubject.Dispose();

            issueChangedPublisherMock.Verify(ch => ch.Dispose(), Times.Once);
            taintPublisherMock.Verify(ch => ch.Dispose(), Times.Once);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerSentEventPump, IServerSentEventPump>(
                MefTestHelpers.CreateExport<IIssueChangedServerEventSourcePublisher>(),
                MefTestHelpers.CreateExport<ITaintServerEventSourcePublisher>());
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

            await testSubject.PumpAllAsync(serverSentEventsSessionMock.Object);
            
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
        public async Task PumpAllAsync_WhenPublisherDisposed_Finishes()
        {
            SetUpEventsSequenceThatFinishes(Mock.Of<IIssueChangedServerEvent>(), Mock.Of<IIssueChangedServerEvent>());
            issueChangedPublisherMock.Setup(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()))
                .Throws(new ObjectDisposedException(string.Empty));

            await testSubject.PumpAllAsync(serverSentEventsSessionMock.Object);

            issueChangedPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()), Times.Once);
            taintPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<ITaintServerEvent>()), Times.Never);
        }

        private void SetUpEventsSequenceThatFinishes(params IServerEvent[] inputSequence)
        {
            var sequenceSetUp = serverSentEventsSessionMock.SetupSequence(x => x.ReadAsync());
            sequenceSetUp = inputSequence.Aggregate(sequenceSetUp, (current, serverEvent) => current.ReturnsAsync(serverEvent));
            sequenceSetUp.Throws<OperationCanceledException>();
        }
    }
}
