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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Taint;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Issue;

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
        
        CheckEventsSequence<ITaintServerEvent>(testScope.TaintPublisherMock.Invocations);
        CheckEventsSequence<IIssueChangedServerEvent>(testScope.IssuePublisherMock.Invocations);

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
            new IServerEvent[]
            {
                Mock.Of<ITaintVulnerabilityRaisedServerEvent>(),
                null,
                Mock.Of<ITaintVulnerabilityRaisedServerEvent>(),
                Mock.Of<IIssueChangedServerEvent>(),
                Mock.Of<IIssueChangedServerEvent>()
            });

        await testScope.TestSubject.PumpAllAsync();

        testScope.TaintPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<ITaintServerEvent>()), Times.Exactly(2));
        testScope.IssuePublisherMock.Verify(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task PumpAllAsync_WhenUnsupportedEvent_Ignores()
    {
        var testScope = new TestScope();
        testScope.SetUpSwitchToBackgroundThread();
        var sseStreamMock = testScope.SetUpSQServiceToSuccessfullyReturnSSEStreamReader();

        testScope.SetUpSSEStreamReaderToReturnEventsSequenceAndExit(sseStreamMock,
            new IServerEvent[]
            {
                Mock.Of<ITaintVulnerabilityRaisedServerEvent>(),
                Mock.Of<IDummyServerEvent>(),
                Mock.Of<ITaintVulnerabilityRaisedServerEvent>(),
                Mock.Of<IIssueChangedServerEvent>()
            });

        await testScope.TestSubject.PumpAllAsync();

        testScope.TaintPublisherMock.Verify(publisher => publisher.Publish(It.IsAny<ITaintServerEvent>()), Times.Exactly(2));
        testScope.IssuePublisherMock.Verify(publisher => publisher.Publish(It.IsAny<IIssueChangedServerEvent>()), Times.Exactly(1));
    }

    [TestMethod]
    public async Task PumpAllAsync_WhenPublisherErrors_NonCriticalError_ErrorLoggedAndSessionIsDisposed()
    {
        var testScope = new TestScope();
        testScope.SetUpSwitchToBackgroundThread();
        var sseStreamMock = testScope.SetUpSQServiceToSuccessfullyReturnSSEStreamReader();
        sseStreamMock.Setup(x => x.ReadAsync()).Throws(new NotImplementedException("this is a test"));

        testScope.LoggerMock.Setup(x => x.LogVerbose(It.Is<string>(s => s.Contains("this is a test")), Array.Empty<object>()));

        await testScope.TestSubject.PumpAllAsync();

        testScope.LoggerMock.Verify(x=> x.LogVerbose(It.Is<string>(s=> s.Contains("this is a test")), Array.Empty<object>()), Times.Once);
        testScope.CapturedSessionToken.Value.IsCancellationRequested.Should().BeTrue();
    }

    [TestMethod]
    public void PumpAllAsync_WhenPublisherErrors_CriticalError_SessionThrows()
    {
        var testScope = new TestScope();
        testScope.SetUpSwitchToBackgroundThread();
        var sseStreamMock = testScope.SetUpSQServiceToSuccessfullyReturnSSEStreamReader();
        sseStreamMock.Setup(x => x.ReadAsync()).Throws(new DivideByZeroException("this is a test"));

        Func<Task> func = async () => await testScope.TestSubject.PumpAllAsync();

        func.Should().ThrowExactly<DivideByZeroException>().And.Message.Should().Be("this is a test");
        testScope.LoggerMock.Invocations.Count.Should().Be(0);
        testScope.CapturedSessionToken.Value.IsCancellationRequested.Should().BeFalse();
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

    public interface IDummyServerEvent : IServerEvent { }

    private class TestScope
    {
        private readonly MockRepository mockRepository;

        public TestScope()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);
            SonarQubeServiceMock = mockRepository.Create<ISonarQubeService>();
            TaintPublisherMock = mockRepository.Create<ITaintServerEventSourcePublisher>(MockBehavior.Loose);
            IssuePublisherMock = mockRepository.Create<IIssueServerEventSourcePublisher>(MockBehavior.Loose);
            ThreadHandlingMock = mockRepository.Create<IThreadHandling>();
            LoggerMock = new Mock<ILogger>();

            var factory = new SSESessionFactory(
                SonarQubeServiceMock.Object,
                TaintPublisherMock.Object,
                IssuePublisherMock.Object,
                ThreadHandlingMock.Object,
                LoggerMock.Object);

            TestSubject = factory.Create("blalala");
        }

        private Mock<IThreadHandling> ThreadHandlingMock { get; }
        public Mock<ISonarQubeService> SonarQubeServiceMock { get; }
        public Mock<ITaintServerEventSourcePublisher> TaintPublisherMock { get; }
        public Mock<IIssueServerEventSourcePublisher> IssuePublisherMock { get; }
        public Mock<ILogger> LoggerMock { get; }
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
