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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarQube.Client;
using SonarQube.Client.Models.ServerSentEvents;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.SeverSentEvents
{
    [TestClass]
    public class ServerSentEventSessionManagerTests
    {
        private static readonly ActiveSolutionBindingEventArgs ClosedProjectEvent =
            new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone);

        private CancellationToken sessionToken;
        private MockSequence callSequence;
        private Mock<IActiveSolutionBoundTracker> activeSolutionBoundTrackerMock;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private Mock<IServerSentEventPump> serverSentEventPumpMock;
        private Mock<IIssueChangedServerEventSourcePublisher> issueChangedServerEventSourcePublisherMock;
        private Mock<ITaintServerEventSourcePublisher> taintServerEventSourcePublisher;
        private Mock<IThreadHandling> threadHandlingMock;

        private ServerSentEventSessionManager testSubject;

        [TestInitialize]
        public void SetUp()
        {
            callSequence = new MockSequence();

            activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>(MockBehavior.Strict);
            sonarQubeServiceMock = new Mock<ISonarQubeService>(MockBehavior.Strict);
            serverSentEventPumpMock = new Mock<IServerSentEventPump>(MockBehavior.Strict);
            issueChangedServerEventSourcePublisherMock = new Mock<IIssueChangedServerEventSourcePublisher>(MockBehavior.Strict);
            taintServerEventSourcePublisher = new Mock<ITaintServerEventSourcePublisher>(MockBehavior.Strict);
            threadHandlingMock = new Mock<IThreadHandling>(MockBehavior.Strict);

            threadHandlingMock
                .InSequence(callSequence)
                .Setup(threadHandling => threadHandling.RunOnBackgroundThread<bool>(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> arg1) => arg1.Invoke());

            testSubject = new ServerSentEventSessionManager(activeSolutionBoundTrackerMock.Object,
                sonarQubeServiceMock.Object,
                serverSentEventPumpMock.Object,
                issueChangedServerEventSourcePublisherMock.Object,
                taintServerEventSourcePublisher.Object,
                threadHandlingMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerSentEventSessionManager, IServerSentEventSessionManager>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<ITaintServerEventSourcePublisher>(),
                MefTestHelpers.CreateExport<IIssueChangedServerEventSourcePublisher>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<IServerSentEventPump>());
        }

        [TestMethod]
        public void Ctor_SubscribesToSolutionEvents()
        {
            var solutionTrackerMock = new Mock<IActiveSolutionBoundTracker>();

            var serverSentEventSessionManager = new ServerSentEventSessionManager(solutionTrackerMock.Object, Mock.Of<ISonarQubeService>(),
                Mock.Of<IServerSentEventPump>(), Mock.Of<IIssueChangedServerEventSourcePublisher>(),
                Mock.Of<ITaintServerEventSourcePublisher>(), Mock.Of<IThreadHandling>());

            solutionTrackerMock.VerifyAdd(solutionTracker => solutionTracker.SolutionBindingChanged += serverSentEventSessionManager.OnSolutionChanged, Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_CorrectlyIdentifiesOpenAndClose(bool isOpen)
        {
            var currentConfiguration = isOpen ? CreateNewOpenProjectEvent() : ClosedProjectEvent;

            testSubject.OnSolutionChanged(null, currentConfiguration);

            sonarQubeServiceMock.Verify(
                sonarQubeService =>
                    sonarQubeService.CreateServerSentEventsSession(
                        It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(isOpen ? 1 : 0));
        }

        [TestMethod]
        public void OnSolutionChanged_WhenChangesFromClosedToOpen_CreatesSessionAndPassesItToThePump()
        {
            var openConfiguration = CreateNewOpenProjectEvent();
            SetUpMockConnection();
            testSubject.OnSolutionChanged(null, ClosedProjectEvent);

            testSubject.OnSolutionChanged(null, openConfiguration);

            sonarQubeServiceMock.Verify(
                sonarQubeService =>
                    sonarQubeService.CreateServerSentEventsSession(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
            serverSentEventPumpMock.Verify(
                serverEventPump =>
                    serverEventPump.PumpAllAsync(It.IsAny<IServerSentEventsSession>(), It.IsAny<IIssueChangedServerEventSourcePublisher>(), It.IsAny<ITaintServerEventSourcePublisher>()),
                Times.Once);
        }

        [TestMethod]
        public void OnSolutionChanged_WhenChangesFromOpenToClosed_CancelsSession()
        {
            SetUpOpenProject();

            testSubject.OnSolutionChanged(null, ClosedProjectEvent);

            sessionToken.IsCancellationRequested.Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_WhenProjectOpened_CorrectAndIdempotent()
        {
            SetUpMocksDispose();
            SetUpOpenProject();
            
            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            activeSolutionBoundTrackerMock.VerifyRemove(
                activeSolutionBoundTracker => activeSolutionBoundTracker.SolutionBindingChanged -=
                    testSubject.OnSolutionChanged, Times.Once);
            sessionToken.IsCancellationRequested.Should().BeTrue();
            issueChangedServerEventSourcePublisherMock.Verify(ch => ch.Dispose(), Times.Once);
            taintServerEventSourcePublisher.Verify(ch => ch.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Dispose_WhenProjectClosed_CorrectAndIdempotent()
        {
            SetUpMocksDispose();
            testSubject.OnSolutionChanged(null, ClosedProjectEvent);

            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            activeSolutionBoundTrackerMock.VerifyRemove(
                activeSolutionBoundTracker => activeSolutionBoundTracker.SolutionBindingChanged -=
                    testSubject.OnSolutionChanged, Times.Once);
            sessionToken.Should().Be(CancellationToken.None);
            issueChangedServerEventSourcePublisherMock.Verify(ch => ch.Dispose(), Times.Once);
            taintServerEventSourcePublisher.Verify(ch => ch.Dispose(), Times.Once);
        }

        // todo(georgii-borovinskikh): test sqs error handling when we finalize it

        private ActiveSolutionBindingEventArgs CreateNewOpenProjectEvent()
        {
            var randomString = Guid.NewGuid().ToString();
            return new ActiveSolutionBindingEventArgs(new BindingConfiguration(
                new BoundSonarQubeProject(new Uri("http://localhost"), randomString, randomString),
                SonarLintMode.Connected,
                randomString));
        }

        private void SetUpOpenProject()
        {
            var openConfiguration = CreateNewOpenProjectEvent();
            sonarQubeServiceMock.Setup(sonarQubeService =>
                    sonarQubeService.CreateServerSentEventsSession(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(
                    (string _, CancellationToken arg2) =>
                    {
                        sessionToken = arg2;
                        return Task.FromResult(Mock.Of<IServerSentEventsSession>());
                    });
            testSubject.OnSolutionChanged(null, openConfiguration);
        }

        private void SetUpMocksDispose()
        {
            issueChangedServerEventSourcePublisherMock.Setup(ch => ch.Dispose());
            taintServerEventSourcePublisher.Setup(ch => ch.Dispose());
        }

        private IServerSentEventsSession SetUpMockConnection()
        {
            var session = Mock.Of<IServerSentEventsSession>();
            sonarQubeServiceMock
                .InSequence(callSequence)
                .Setup(sonarQubeService =>
                    sonarQubeService.CreateServerSentEventsSession(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            serverSentEventPumpMock
                .InSequence(callSequence)
                .Setup(serverSentEventPump => serverSentEventPump.PumpAllAsync(session,
                    issueChangedServerEventSourcePublisherMock.Object, taintServerEventSourcePublisher.Object))
                .Returns(Task.CompletedTask);
            return session;
        }
    }
}
