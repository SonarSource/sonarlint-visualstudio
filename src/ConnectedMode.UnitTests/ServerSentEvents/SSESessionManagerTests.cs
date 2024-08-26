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

using System;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents;

[TestClass]
public class SSESessionManagerTests
{
    private const string DefaultProjectKey = "myproj";

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        var activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>();
        activeSolutionBoundTrackerMock.SetupGet(tracker => tracker.CurrentConfiguration)
            .Returns(BindingConfiguration.Standalone);

        MefTestHelpers.CheckTypeCanBeImported<SSESessionManager, SSESessionManager>(
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(activeSolutionBoundTrackerMock.Object),
            MefTestHelpers.CreateExport<ISSESessionFactory>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void Ctor_DoesNotCallAnyServices_BesidesExpected()
    {
        var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
        var sseSessionFactory = new Mock<ISSESessionFactory>();
        var logger = new Mock<ILogger>();

        var _ = new SSESessionManager(activeSolutionBoundTracker.Object, sseSessionFactory.Object, logger.Object);

        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls.

        activeSolutionBoundTracker.VerifyAdd(tracker => tracker.SolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
        activeSolutionBoundTracker.VerifyNoOtherCalls();
        sseSessionFactory.Invocations.Should().BeEmpty();
        logger.Invocations.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_SubscribesToBindingChangedEvent()
    {
        var testScope = new TestScope();

        var _ = testScope.CreateTestSubject();

        testScope.ActiveSolutionBoundTrackerMock.VerifyAdd(
            tracker => tracker.SolutionBindingChanged +=
                It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
    }

    [TestMethod]
    public void CreateSessionIfInConnectedMode_WhenInStandaloneModeOnCreation_DoesNotCreateSession()
    {
        var bindingConfig = BindingConfiguration.Standalone;
        var testScope = new TestScope(bindingConfig);

        var _ = testScope.CreateTestSubject();
        var testSubject = testScope.CreateTestSubject();

        testSubject.CreateSessionIfInConnectedMode(bindingConfig);

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey, It.IsAny<OnSessionFailedAsync>()), Times.Never);
    }

    [TestMethod]
    public void CreateSessionIfInConnectedMode_WhenInConnectedModeOnCreation_CreatesSession()
    {
        var bindingConfig = TestScope.CreateConnectedModeBindingConfiguration(DefaultProjectKey);

        var testScope = new TestScope(bindingConfig);
        var sessionMock = testScope.SetUpSSEFactoryToReturnNoOpSSESession(DefaultProjectKey);

        var testSubject = testScope.CreateTestSubject();

        testSubject.CreateSessionIfInConnectedMode(bindingConfig);
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey, It.IsAny<OnSessionFailedAsync>()), Times.Once);
        sessionMock.Verify(session => session.PumpAllAsync(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromStandaloneToConnected_CreatesSessionAndLaunchesIt()
    {
        var testScope = new TestScope();
        var _ = testScope.CreateTestSubject();
        testScope.RaiseInStandaloneModeEvent();
        var sessionMock = testScope.SetUpSSEFactoryToReturnNoOpSSESession(DefaultProjectKey);

        testScope.RaiseInConnectedModeEvent(DefaultProjectKey);

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey, It.IsAny<OnSessionFailedAsync>()), Times.Once);
        sessionMock.Verify(session => session.PumpAllAsync(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromConnectedToStandalone_DisposesPreviousSession()
    {
        var testScope = new TestScope();
        var _ = testScope.CreateTestSubject();
        var sessionMock = testScope.SetUpSSEFactoryToReturnNoOpSSESession(DefaultProjectKey);
        sessionMock.Setup(session => session.Dispose());
        testScope.RaiseInConnectedModeEvent(DefaultProjectKey);
        testScope.SSESessionFactoryMock.Invocations.Clear();

        testScope.RaiseInStandaloneModeEvent();

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(It.IsAny<string>(), It.IsAny<OnSessionFailedAsync>()), Times.Never);
        sessionMock.Verify(session => session.Dispose(), Times.Once);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void OnSolutionChanged_WhenChangesFromConnectedToConnected_CancelsSessionAndStartsNewOne(bool sameProjectKey)
    {
        var projectKey1 = "proj1";
        var projectKey2 = sameProjectKey ? projectKey1 : "proj2";
        var testScope = new TestScope();
        var _ = testScope.CreateTestSubject();
        var sessionMock1 = testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey1);
        sessionMock1.Setup(session => session.Dispose());
        testScope.RaiseInConnectedModeEvent(projectKey1);
        var sessionMock2 = testScope.SetUpSSEFactoryToReturnNoOpSSESession(projectKey2);

        testScope.RaiseInConnectedModeEvent(projectKey2);

        sessionMock1.Verify(session => session.Dispose(), Times.Once);
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(projectKey2, It.IsAny<OnSessionFailedAsync>()), Times.Exactly(sameProjectKey ? 2 : 1));
        sessionMock2.Verify(session => session.Dispose(), Times.Never);
    }

    [DataTestMethod]
    public async Task OnSessionFailed_CancelsSessionAndStartsNewOne()
    {
        var bindingConfig = TestScope.CreateConnectedModeBindingConfiguration(DefaultProjectKey);

        var testScope = new TestScope(bindingConfig);

        var sessionMock1 = testScope.SetUpSSEFactoryToReturnNoOpSSESession(DefaultProjectKey);
        sessionMock1.Setup(session => session.Dispose());
        var testSubject = testScope.CreateTestSubject();
        testSubject.CreateSessionIfInConnectedMode(bindingConfig);

        var sessionMock2 = testScope.SetUpSSEFactoryToReturnNoOpSSESession(DefaultProjectKey);

        await testScope.CapturedSessionFailedCallback(sessionMock1.Object);

        sessionMock1.Verify(session => session.Dispose(), Times.Once);
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey, It.IsAny<OnSessionFailedAsync>()), Times.Exactly(2));
        sessionMock2.Verify(session => session.Dispose(), Times.Never);
    }

    [TestMethod]
    public void Dispose_WhenInConnectedMode_CorrectAndIdempotent()
    {
        var testScope = new TestScope();
        var testSubject = testScope.CreateTestSubject();
        var sseSession = testScope.SetUpSSEFactoryToReturnNoOpSSESession(DefaultProjectKey);
        testScope.SetUpCorrectDisposeOrder(sseSession);
        testScope.RaiseInConnectedModeEvent(DefaultProjectKey);

        CallDisposeMultipleTimes(testSubject);

        VerifyUnsubscribedFromBindingChangedEvent(testScope);
        VerifySSESessionFactoryDisposedOnce(testScope);
        sseSession.Verify(session => session.Dispose(), Times.Once);
    }

    [TestMethod]
    public void Dispose_WhenInStandaloneMode_CorrectAndIdempotent()
    {
        var testScope = new TestScope();
        var testSubject = testScope.CreateTestSubject();
        testScope.SetUpCorrectDisposeOrder(null);
        testScope.RaiseInStandaloneModeEvent();

        CallDisposeMultipleTimes(testSubject);

        VerifyUnsubscribedFromBindingChangedEvent(testScope);
        VerifySSESessionFactoryDisposedOnce(testScope);
    }

    private static void VerifySSESessionFactoryDisposedOnce(TestScope testScope)
    {
        testScope.SSESessionFactoryMock.Verify(sessionFactory => sessionFactory.Dispose(), Times.Once);
    }

    private static void VerifyUnsubscribedFromBindingChangedEvent(TestScope testScope)
    {
        testScope.ActiveSolutionBoundTrackerMock.VerifyRemove(
            tracker =>
                tracker.SolutionBindingChanged -=
                    It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(),
            Times.Once);
    }

    private static void CallDisposeMultipleTimes(SSESessionManager testSubject)
    {
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();
    }

    private class TestScope
    {
        private readonly MockRepository mockRepository;
        private readonly MockSequence callOrder = new();

        public TestScope(BindingConfiguration initialBindingState = null)
        {
            mockRepository = new MockRepository(MockBehavior.Strict);
            ActiveSolutionBoundTrackerMock = mockRepository.Create<IActiveSolutionBoundTracker>();
            SSESessionFactoryMock = mockRepository.Create<ISSESessionFactory>();

            // This is not in a sequence so we can call it multiple times
            ActiveSolutionBoundTrackerMock
                .SetupGet(tracker => tracker.CurrentConfiguration)
                .Returns(initialBindingState ?? BindingConfiguration.Standalone);
        }

        public Mock<IActiveSolutionBoundTracker> ActiveSolutionBoundTrackerMock { get; }
        public Mock<ISSESessionFactory> SSESessionFactoryMock { get; }
        public OnSessionFailedAsync CapturedSessionFailedCallback { get; private set; }

        public SSESessionManager CreateTestSubject()
        {
            return new SSESessionManager(
                ActiveSolutionBoundTrackerMock.Object,
                SSESessionFactoryMock.Object,
                Mock.Of<ILogger>());
        }

        public static BindingConfiguration CreateConnectedModeBindingConfiguration(string projectKey)
        {
            var randomString = Guid.NewGuid().ToString();
            var bindingConfiguration = new BindingConfiguration(
                new BoundServerProject(randomString, projectKey, new ServerConnection.SonarQube(new Uri("http://localhost"))),
                SonarLintMode.Connected,
                randomString);
            return bindingConfiguration;
        }

        public void SetUpCorrectDisposeOrder(Mock<ISSESession> currentSession)
        {
            ActiveSolutionBoundTrackerMock.SetupRemove(tracker =>
                tracker.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>());

            SSESessionFactoryMock.InSequence(callOrder).Setup(sessionFactory => sessionFactory.Dispose());

            currentSession?.InSequence(callOrder).Setup(session => session.Dispose());
        }

        public Mock<ISSESession> SetUpSSEFactoryToReturnNoOpSSESession(string projectKey, Action factoryMockCallback = null)
        {
            var sseSessionMock = mockRepository.Create<ISSESession>();

            SSESessionFactoryMock.InSequence(callOrder)
                 .Setup(sessionFactory => sessionFactory.Create(projectKey, It.IsAny<OnSessionFailedAsync>()))
                 .Returns(sseSessionMock.Object)
                 .Callback((string projectKey,OnSessionFailedAsync callbackAction) => {
                     CapturedSessionFailedCallback = callbackAction;
                     factoryMockCallback?.Invoke();
                     });

            sseSessionMock
                .InSequence(callOrder)
                .Setup(session => session.PumpAllAsync())
                .Returns(Task.CompletedTask);

            return sseSessionMock;
        }

        public void RaiseInConnectedModeEvent(string projectKey)
        {
            var openProjectEvent = CreateConnectedModeBindingConfiguration(projectKey);
            RaiseSolutionBindingEvent(openProjectEvent);
        }

        public void RaiseInStandaloneModeEvent()
        {
            RaiseSolutionBindingEvent(BindingConfiguration.Standalone);
        }

        private void RaiseSolutionBindingEvent(BindingConfiguration bindingConfiguration)
        {
            ActiveSolutionBoundTrackerMock.Raise(tracker => tracker.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(bindingConfiguration));
        }
    }
}
