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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarQube.Client;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents;

[TestClass]
public class SSESessionTests
{
    [TestMethod]
    public void PumpAllAsync_WhenSonarQubeRefusesConnection_DoesNotThrow()
    {
        var testScope = new TestScope();
        testScope.SetUpSwitchToBackgroundThread();
        testScope.SonarQubeServiceMock
            .InSequence(testScope.CallOrder)
            .Setup(sqs => sqs.CreateSSEStreamReader(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ISSEStreamReader)null);
        
        Func<Task> act = () => testScope.TestSubject.PumpAllAsync();

        act.Should().NotThrow();
    }


    [TestMethod]
    public async Task PumpAllAsync_SelectsPublisherCorrectlyAndPreservesOrderWithinType()
    {
        var testScope = new TestScope();
        var inputSequence = new IServerEvent[]
        {
            Mock.Of<IIssueChangedServerEvent>(),
            Mock.Of<ITaintVulnerabilityRaisedServerEvent>(),
            Mock.Of<IIssueChangedServerEvent>(),
            Mock.Of<ITaintVulnerabilityClosedServerEvent>(),
            Mock.Of<ITaintVulnerabilityClosedServerEvent>(),
            Mock.Of<IIssueChangedServerEvent>(),
            Mock.Of<ITaintVulnerabilityRaisedServerEvent>()
        };
        testScope.SetUpSwitchToBackgroundThread();
        var sseStreamMock = testScope.SetUpSQServiceToSuccessfullyReturnSSEStreamReader();
        testScope.SetUpSSEStreamReaderToReturnEventsSequenceAndExit(sseStreamMock, inputSequence);

        await testScope.TestSubject.PumpAllAsync();
        
        CheckEventsSequence<IIssueChangedServerEvent>(testScope.IssuesPublisherMock.Invocations);
        CheckEventsSequence<ITaintServerEvent>(testScope.TaintPublisherMock.Invocations);

        // note: can't pass Mock<IServerSentEventSourcePublisher<T>> because Mock's generic type is not covariant and we use the publisher interface through inheritor interfaces
        void CheckEventsSequence<T>(IEnumerable<IInvocation> publisherMockInvocations) where T : class, IServerEvent
        {
            var publishedEvents = publisherMockInvocations.Select(call => call.Arguments.First() as T);
            var expectedPublishedEvents = inputSequence.Where(issuesEvent => issuesEvent is T);

            publishedEvents.Should().BeEquivalentTo(expectedPublishedEvents);
        }
    }

    [TestMethod]
    public async Task PumpAllAsync_WhenNullEvent_Ignores()
    {
        var testScope = new TestScope();
        testScope.SetUpSwitchToBackgroundThread();
        var sseStreamMock = testScope.SetUpSQServiceToSuccessfullyReturnSSEStreamReader();
        testScope.SetUpSSEStreamReaderToReturnEventsSequenceAndExit(sseStreamMock, 
            new IServerEvent[]{ Mock.Of<IIssueChangedServerEvent>(), null, Mock.Of<ITaintVulnerabilityRaisedServerEvent>()});

        await testScope.TestSubject.PumpAllAsync();

        testScope.IssuesPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()),
            Times.Once);
        testScope.TaintPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<ITaintServerEvent>()), Times.Once);
    }

    [TestMethod]
    public async Task PumpAllAsync_WhenPublisherDisposed_Finishes()
    {
        var testScope = new TestScope();
        testScope.SetUpSwitchToBackgroundThread();
        var sseStreamMock = testScope.SetUpSQServiceToSuccessfullyReturnSSEStreamReader();
        testScope.SetUpSSEStreamReaderToReturnEventsSequenceAndExit(sseStreamMock, 
            new IServerEvent[]{Mock.Of<IIssueChangedServerEvent>(), Mock.Of<ITaintVulnerabilityRaisedServerEvent>()});
        testScope.IssuesPublisherMock.Setup(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()))
            .Throws(new ObjectDisposedException(string.Empty));

        await testScope.TestSubject.PumpAllAsync();

        testScope.IssuesPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()),
            Times.Once);
        testScope.TaintPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<ITaintServerEvent>()), Times.Never);
    }

    [TestMethod]
    public void PumpAllAsync_AfterDisposed_Throws()
    {
        var testScope = new TestScope();
        testScope.TestSubject.Dispose();

        var act = () => testScope.TestSubject.PumpAllAsync();

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task Dispose_FinishesSession()
    {
        var testScope = new TestScope();
        testScope.SetUpSwitchToBackgroundThread();
        var sseStreamMock = testScope.SetUpSQServiceToSuccessfullyReturnSSEStreamReader();
        var readTcs = new TaskCompletionSource<IServerEvent>();
        sseStreamMock.Setup(x => x.ReadAsync()).Returns(readTcs.Task);

        var pumpTask = testScope.TestSubject.PumpAllAsync();
        testScope.TestSubject.Dispose();
        readTcs.SetResult(null);
        await pumpTask;

        var sessionToken = testScope.CapturedSessionToken;
        sessionToken.Should().NotBeNull();
        sessionToken.Value.IsCancellationRequested.Should().BeTrue();
    }

    private class TestScope
    {
        private readonly MockRepository mockRepository;

        public TestScope()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);
            SonarQubeServiceMock = mockRepository.Create<ISonarQubeService>();
            IssuesPublisherMock = mockRepository.Create<IIssueChangedServerEventSourcePublisher>(MockBehavior.Loose);
            TaintPublisherMock = mockRepository.Create<ITaintServerEventSourcePublisher>(MockBehavior.Loose);
            ThreadHandlingMock = mockRepository.Create<IThreadHandling>();

            var factory = new SSESessionFactory(
                SonarQubeServiceMock.Object,
                IssuesPublisherMock.Object,
                TaintPublisherMock.Object,
                ThreadHandlingMock.Object);
            TestSubject = factory.Create("blalala");
        }

        private Mock<IThreadHandling> ThreadHandlingMock { get; }
        public Mock<ISonarQubeService> SonarQubeServiceMock { get; }
        public Mock<IIssueChangedServerEventSourcePublisher> IssuesPublisherMock { get; }
        public Mock<ITaintServerEventSourcePublisher> TaintPublisherMock { get; }
        public CancellationToken? CapturedSessionToken { get; private set; }
        public MockSequence CallOrder { get; } = new MockSequence();
        public ISSESession TestSubject { get; }

        public void SetUpSSEStreamReaderToReturnEventsSequenceAndExit(Mock<ISSEStreamReader> sseStreamMock, IServerEvent[] inputSequence)
        {
            foreach (var serverEvent in inputSequence)
            {
                sseStreamMock
                    .InSequence(CallOrder)
                    .Setup(r => r.ReadAsync())
                    .ReturnsAsync(serverEvent);
            }

            sseStreamMock
                .InSequence(CallOrder)
                .Setup(r => r.ReadAsync())
                .ReturnsAsync(() =>
                {
                    TestSubject.Dispose();
                    return null;
                });
        }

        public Mock<ISSEStreamReader> SetUpSQServiceToSuccessfullyReturnSSEStreamReader()
        {
            var sseStreamMock = mockRepository.Create<ISSEStreamReader>();

            SonarQubeServiceMock
                .InSequence(CallOrder)
                .Setup(client => client.CreateSSEStreamReader(It.IsAny<string>(),
                    It.Is<CancellationToken>(token => token != CancellationToken.None)))
                .ReturnsAsync((string _, CancellationToken tokenArg) =>
                {
                    CapturedSessionToken = tokenArg;
                    return sseStreamMock.Object;
                });

            return sseStreamMock;
        }

        public void SetUpSwitchToBackgroundThread()
        {
            ThreadHandlingMock
                .InSequence(CallOrder)
                .Setup(th => th.SwitchToBackgroundThread())
                .Returns(new NoOpThreadHandler.NoOpAwaitable());
        }
    }
}
