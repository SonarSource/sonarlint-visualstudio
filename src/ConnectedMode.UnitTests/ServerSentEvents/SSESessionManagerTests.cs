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
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents;

[TestClass]
public class SSESessionManagerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SSESessionManager, ISSESessionManager>(
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<ISSESessionFactory>());
    }

    [TestMethod]
    public void Ctor_SubscribesToBindingChangedEvent()
    {
        var solutionTrackerMock = new Mock<IActiveSolutionBoundTracker>();

        var _ = new SSESessionManager(solutionTrackerMock.Object, Mock.Of<ISSESessionFactory>());

        solutionTrackerMock.VerifyAdd(
            solutionTracker => solutionTracker.SolutionBindingChanged +=
                It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void OnSolutionChanged_CorrectlyIdentifiesOpenAndClose(bool isOpen)
    {
        var testScope = new TestScope();
        testScope.SetUpMockConnection();

        if (isOpen)
        {
            testScope.OpenNewProject();
        }
        else
        {
            testScope.CloseProject();
        }

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(It.IsAny<string>()),
            Times.Exactly(isOpen ? 1 : 0));
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromClosedToOpen_CreatesSessionAndLaunchesIt()
    {
        var testScope = new TestScope();
        testScope.CloseProject();
        var sessionMock = testScope.SetUpMockConnection();

        testScope.OpenNewProject();

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(It.IsAny<string>()), Times.Once);
        sessionMock.Verify(session => session.PumpAllAsync(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromOpenToClosed_CancelsSession()
    {
        var testScope = new TestScope();
        var sessionMock = testScope.SetUpMockConnection();
        sessionMock.Setup(session => session.Dispose());
        testScope.OpenNewProject();

        testScope.CloseProject();

        sessionMock.Verify(session => session.Dispose(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromOpenToOpen_CancelsSessionAndStartsNewOne()
    {
        var testScope = new TestScope();
        var sessionMock1 = testScope.SetUpMockConnection();
        sessionMock1.Setup(session => session.Dispose());
        testScope.OpenNewProject();
        var sessionMock2 = testScope.SetUpMockConnection();

        testScope.OpenNewProject();

        sessionMock1.Verify(session => session.Dispose(), Times.Once);
        sessionMock2.Verify(session => session.Dispose(), Times.Never);
    }

    [TestMethod]
    public void Dispose_WhenProjectOpened_CorrectAndIdempotent()
    {
        var testScope = new TestScope();
        var mockConnection = testScope.SetUpMockConnection();
        testScope.SetUpMocksDispose(mockConnection);
        testScope.OpenNewProject();

        CallDisposeMultipleTimes(testScope);

        TestDisposal(testScope);
    }

    [TestMethod]
    public void Dispose_WhenProjectClosed_CorrectAndIdempotent()
    {
        var testScope = new TestScope();
        testScope.SetUpMocksDispose(null);
        testScope.CloseProject();

        CallDisposeMultipleTimes(testScope);

        TestDisposal(testScope);
    }

    private static void TestDisposal(TestScope testConfiguration)
    {
        testConfiguration.ActiveSolutionBoundTrackerMock.VerifyRemove(
            activeSolutionBoundTracker =>
                activeSolutionBoundTracker.SolutionBindingChanged -=
                    It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(),
            Times.Once);
        testConfiguration.SSESessionFactoryMock.Verify(sessionFactory => sessionFactory.Dispose(), Times.Once);
    }

    private static void CallDisposeMultipleTimes(TestScope testScope)
    {
        testScope.SessionManager.Dispose();
        testScope.SessionManager.Dispose();
        testScope.SessionManager.Dispose();
    }

    private class TestScope
    {
        private static readonly ActiveSolutionBindingEventArgs
            ClosedProjectEvent = new(BindingConfiguration.Standalone);

        private readonly MockRepository mockRepository;

        public TestScope()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);
            ActiveSolutionBoundTrackerMock = mockRepository.Create<IActiveSolutionBoundTracker>();
            SSESessionFactoryMock = mockRepository.Create<ISSESessionFactory>();
            SessionManager = new SSESessionManager(
                ActiveSolutionBoundTrackerMock.Object,
                SSESessionFactoryMock.Object);
        }

        public Mock<IActiveSolutionBoundTracker> ActiveSolutionBoundTrackerMock { get; }
        public Mock<ISSESessionFactory> SSESessionFactoryMock { get; }
        public SSESessionManager SessionManager { get; }

        public void SetUpMocksDispose(Mock<ISSESession> currentSession, MockSequence callSequence = null)
        {
            callSequence ??= new MockSequence();
            ActiveSolutionBoundTrackerMock.SetupRemove(tracker =>
                tracker.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>());

            SSESessionFactoryMock.InSequence(callSequence).Setup(sessionFactory => sessionFactory.Dispose());

            if (currentSession != null)
            {
                currentSession.InSequence(callSequence).Setup(session => session.Dispose());
            }
        }

        public Mock<ISSESession> SetUpMockConnection(MockSequence callSequence = null)
        {
            callSequence ??= new MockSequence();

            var sseSessionMock = mockRepository.Create<ISSESession>();
            SSESessionFactoryMock.InSequence(callSequence)
                .Setup(sessionFactory => sessionFactory.Create(It.IsAny<string>())).Returns(sseSessionMock.Object);

            sseSessionMock.InSequence(callSequence).Setup(session => session.PumpAllAsync())
                .Returns(Task.CompletedTask);

            return sseSessionMock;
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
            ActiveSolutionBoundTrackerMock.Raise(tracker => tracker.SolutionBindingChanged += null, args);
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
