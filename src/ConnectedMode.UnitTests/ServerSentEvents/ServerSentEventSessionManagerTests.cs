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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarQube.Client;
using SonarQube.Client.Models.ServerSentEvents;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents
{
    [TestClass]
    public class ServerSentEventSessionManagerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerSentEventSessionManager, IServerSentEventSessionManager>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<IServerSentEventPump>());
        }

        [TestMethod]
        public void Ctor_SubscribesToSolutionEvents()
        {
            var solutionTrackerMock = new Mock<IActiveSolutionBoundTracker>();

            var testSubject = new ServerSentEventSessionManager(solutionTrackerMock.Object, Mock.Of<ISonarQubeService>(),
                Mock.Of<IServerSentEventPump>(), Mock.Of<IThreadHandling>());

            solutionTrackerMock.VerifyAdd(solutionTracker => solutionTracker.SolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_CorrectlyIdentifiesOpenAndClose(bool isOpen)
        {
            var testConfiguration = new TestScope();
            testConfiguration.SetUpMockConnection();

            if (isOpen)
            {
                testConfiguration.OpenNewProject();
            }
            else
            {
                testConfiguration.CloseProject();
            }

            testConfiguration.SonarQubeServiceMock.Verify(
                sonarQubeService =>
                    sonarQubeService.CreateServerSentEventsSession(
                        It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(isOpen ? 1 : 0));
        }

        [TestMethod]
        public void OnSolutionChanged_WhenChangesFromClosedToOpen_CreatesSessionAndPassesItToThePump()
        {
            var testConfiguration = new TestScope();
            testConfiguration.CloseProject();
            var sessionToOpen = testConfiguration.SetUpMockConnection();

            testConfiguration.OpenNewProject();

            testConfiguration.SonarQubeServiceMock.Verify(
                sonarQubeService =>
                    sonarQubeService.CreateServerSentEventsSession(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
            testConfiguration.ServerSentEventPumpMock.Verify(
                serverEventPump =>
                    serverEventPump.PumpAllAsync(sessionToOpen),
                Times.Once);
        }

        [TestMethod]
        public void OnSolutionChanged_WhenChangesFromOpenToClosed_CancelsSession()
        {
            var testConfiguration = new TestScope();
            testConfiguration.SetUpMockConnection();
            testConfiguration.OpenNewProject();

            testConfiguration.CloseProject();

            testConfiguration.SessionToken.IsCancellationRequested.Should().BeTrue();
        }

        [TestMethod]
        public void OnSolutionChanged_WhenChangesFromOpenToOpen_CancelsSessionAndStartsNewOne()
        {
            var testConfiguration = new TestScope();
            testConfiguration.SetUpMockConnection();
            testConfiguration.OpenNewProject();
            var firstConnectionSessionToken = testConfiguration.SessionToken;
            testConfiguration.SetUpMockConnection();

            testConfiguration.OpenNewProject();

            firstConnectionSessionToken.IsCancellationRequested.Should().BeTrue();
            testConfiguration.SessionToken.IsCancellationRequested.Should().BeFalse();
        }

        [TestMethod]
        public void Dispose_WhenProjectOpened_CorrectAndIdempotent()
        {
            var testConfiguration = new TestScope();
            testConfiguration.SetUpMocksDispose();
            testConfiguration.SetUpMockConnection();
            testConfiguration.OpenNewProject();

            CallDisposeMultipleTimes(testConfiguration);

            testConfiguration.ActiveSolutionBoundTrackerMock.VerifyRemove(
                activeSolutionBoundTracker =>
                    activeSolutionBoundTracker.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(),
                Times.Once);
            testConfiguration.SessionToken.IsCancellationRequested.Should().BeTrue();
            testConfiguration.ServerSentEventPumpMock.Verify(p => p.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Dispose_WhenProjectClosed_CorrectAndIdempotent()
        {
            var testConfiguration = new TestScope();
            testConfiguration.SetUpMocksDispose();
            testConfiguration.CloseProject();

            CallDisposeMultipleTimes(testConfiguration);

            testConfiguration.ActiveSolutionBoundTrackerMock.VerifyRemove(
                activeSolutionBoundTracker => 
                    activeSolutionBoundTracker.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(),
                Times.Once);
            testConfiguration.SessionToken.Should().Be(CancellationToken.None);
            testConfiguration.ServerSentEventPumpMock.Verify(p => p.Dispose(), Times.Once);
        }

        private static void CallDisposeMultipleTimes(TestScope testScope)
        {
            testScope.SessionManager.Dispose();
            testScope.SessionManager.Dispose();
            testScope.SessionManager.Dispose();
        }

        // todo(georgii-borovinskikh): test sqs error handling when we finalize it

        private class TestScope
        {
            private static readonly ActiveSolutionBindingEventArgs ClosedProjectEvent = new(BindingConfiguration.Standalone);
            public Mock<IActiveSolutionBoundTracker> ActiveSolutionBoundTrackerMock { get; }
            private Mock<IThreadHandling> ThreadHandlingMock { get; }
            public Mock<ISonarQubeService> SonarQubeServiceMock { get; }
            public Mock<IServerSentEventPump> ServerSentEventPumpMock { get; }
            public ServerSentEventSessionManager SessionManager { get; }

            public CancellationToken SessionToken { get; private set; }

            public TestScope()
            {
                var mockRepository = new MockRepository(MockBehavior.Strict);
                ActiveSolutionBoundTrackerMock = mockRepository.Create<IActiveSolutionBoundTracker>();
                SonarQubeServiceMock = mockRepository.Create<ISonarQubeService>();
                ServerSentEventPumpMock = mockRepository.Create<IServerSentEventPump>();
                ThreadHandlingMock = mockRepository.Create<IThreadHandling>();
                SessionManager = new ServerSentEventSessionManager(
                    ActiveSolutionBoundTrackerMock.Object,
                    SonarQubeServiceMock.Object,
                    ServerSentEventPumpMock.Object,
                    ThreadHandlingMock.Object);
            }

            public void SetUpMocksDispose()
            {
                ServerSentEventPumpMock.Setup(p => p.Dispose());
            }

            public IServerSentEventsSession SetUpMockConnection(
                MockSequence callSequence = null)
            {
                callSequence ??= new MockSequence();
                var session = Mock.Of<IServerSentEventsSession>();
                ThreadHandlingMock
                    .InSequence(callSequence)
                    .Setup(threadHandling => threadHandling.SwitchToBackgroundThread())
                    .Returns(new NoOpThreadHandler.NoOpAwaitable());
                SonarQubeServiceMock
                    .InSequence(callSequence)
                    .Setup(sonarQubeService =>
                        sonarQubeService.CreateServerSentEventsSession(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns((string _, CancellationToken arg2) =>
                    {
                        SessionToken = arg2;
                        return Task.FromResult(session);
                    });
                ServerSentEventPumpMock
                    .InSequence(callSequence)
                    .Setup(serverSentEventPump => serverSentEventPump.PumpAllAsync(session))
                    .Returns(Task.CompletedTask);
                return session;
            }

            public void OpenNewProject()
            {
                var openProjectEvent = CreateNewOpenProjectEvent();
                RaiseSolutionBindingEvent(openProjectEvent);
            }

            public void CloseProject()
            {
                RaiseSolutionBindingEvent(ClosedProjectEvent);
            }

            private void RaiseSolutionBindingEvent(ActiveSolutionBindingEventArgs args)
            {
                ActiveSolutionBoundTrackerMock.Raise(s => s.SolutionBindingChanged += null, args);
            }

            private static ActiveSolutionBindingEventArgs CreateNewOpenProjectEvent()
            {
                var randomString = Guid.NewGuid().ToString();
                return new ActiveSolutionBindingEventArgs(new BindingConfiguration(
                    new BoundSonarQubeProject(new Uri("http://localhost"), randomString, randomString),
                    SonarLintMode.Connected,
                    randomString));
            }
        }
    }
}
