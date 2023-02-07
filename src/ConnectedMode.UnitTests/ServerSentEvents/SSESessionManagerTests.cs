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
    public void OnSolutionChanged_CorrectlyIdentifiesOpenAndClose(bool switchToConnectedMode)
    {
        var testScope = new TestScope();


        if (switchToConnectedMode)
        {
            var projectKey = "myproj";
            testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey);
            testScope.SwitchToConnectedMode(projectKey);
        }
        else
        {
            testScope.SwitchToStandaloneMode();
        }

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(It.IsAny<string>()),
            Times.Exactly(switchToConnectedMode ? 1 : 0));
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromClosedToOpen_CreatesSessionAndLaunchesIt()
    {
        var testScope = new TestScope();
        testScope.SwitchToStandaloneMode();
        var projectKey = "myproj";
        var sessionMock = testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey);

        testScope.SwitchToConnectedMode(projectKey);

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(It.IsAny<string>()), Times.Once);
        sessionMock.Verify(session => session.PumpAllAsync(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromOpenToClosed_CancelsSession()
    {
        var testScope = new TestScope();
        var projectKey = "myproj";
        var sessionMock = testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey);
        sessionMock.Setup(session => session.Dispose());
        testScope.SwitchToConnectedMode(projectKey);

        testScope.SwitchToStandaloneMode();

        sessionMock.Verify(session => session.Dispose(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromOpenToOpen_CancelsSessionAndStartsNewOne()
    {
        var testScope = new TestScope();
        var projectKey1 = "proj1";
        var projectKey2 = "proj2";
        var sessionMock1 = testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey1);
        sessionMock1.Setup(session => session.Dispose());
        testScope.SwitchToConnectedMode(projectKey1);
        var sessionMock2 = testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey2);

        testScope.SwitchToConnectedMode(projectKey2);

        sessionMock1.Verify(session => session.Dispose(), Times.Once);
        sessionMock2.Verify(session => session.Dispose(), Times.Never);
    }

    [TestMethod]
    public void Dispose_WhenProjectOpened_CorrectAndIdempotent()
    {
        var testScope = new TestScope();
        var projectKey = "myproj";
        var sseSession = testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey);
        testScope.SetUpCorrectDisposeOrder(sseSession);
        testScope.SwitchToConnectedMode(projectKey);

        CallDisposeMultipleTimes(testScope);

        CheckEverythingIsDisposedOnce(testScope);
    }

    [TestMethod]
    public void Dispose_WhenProjectClosed_CorrectAndIdempotent()
    {
        var testScope = new TestScope();
        testScope.SetUpCorrectDisposeOrder(null);
        testScope.SwitchToStandaloneMode();

        CallDisposeMultipleTimes(testScope);

        CheckEverythingIsDisposedOnce(testScope);
    }

    private static void CheckEverythingIsDisposedOnce(TestScope testConfiguration)
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
        public MockSequence MainCallSequence { get; } = new MockSequence();
        public SSESessionManager SessionManager { get; }

        public void SetUpCorrectDisposeOrder(Mock<ISSESession> currentSession)
        {
            ActiveSolutionBoundTrackerMock.SetupRemove(tracker =>
                tracker.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>());

            SSESessionFactoryMock.InSequence(MainCallSequence).Setup(sessionFactory => sessionFactory.Dispose());

            if (currentSession != null)
            {
                currentSession.InSequence(MainCallSequence).Setup(session => session.Dispose());
            }
        }

        public Mock<ISSESession> SetUpSSEFactoryToReturnNoOpSSESession(string projectKey)
        {
            var sseSessionMock = mockRepository.Create<ISSESession>();
            SSESessionFactoryMock.InSequence(MainCallSequence)
                .Setup(sessionFactory => sessionFactory.Create(projectKey)).Returns(sseSessionMock.Object);

            sseSessionMock.InSequence(MainCallSequence).Setup(session => session.PumpAllAsync())
                .Returns(Task.CompletedTask);

            return sseSessionMock;
        }

        public void SwitchToConnectedMode(string projectKey)
        {
            var openProjectEvent = CreateNewOpenProjectEvent(projectKey);
            RaiseSolutionBindingEvent(openProjectEvent);
        }

        public void SwitchToStandaloneMode()
        {
            RaiseSolutionBindingEvent(ClosedProjectEvent);
        }

        private void RaiseSolutionBindingEvent(ActiveSolutionBindingEventArgs args)
        {
            ActiveSolutionBoundTrackerMock.Raise(tracker => tracker.SolutionBindingChanged += null, args);
        }

        private static ActiveSolutionBindingEventArgs CreateNewOpenProjectEvent(string projectKey)
        {
            var randomString = Guid.NewGuid().ToString();
            return new ActiveSolutionBindingEventArgs(new BindingConfiguration(
                new BoundSonarQubeProject(new Uri("http://localhost"), projectKey, randomString),
                SonarLintMode.Connected,
                randomString));
        }
    }
}
