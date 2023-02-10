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
using Moq.Language.Flow;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.UnitTests;

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

        MefTestHelpers.CheckTypeCanBeImported<SSESessionManager, ISSESessionManager>(
            MefTestHelpers.CreateExport(activeSolutionBoundTrackerMock.Object),
            MefTestHelpers.CreateExport<ISSESessionFactory>());
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
    public void Ctor_ForceConnectsAfterSubscriptionToBindingChangedEvent()
    {
        var isSubscribed = false;
        var testScope = new TestScope(TestScope.CreateConnectedModeBindingConfiguration(DefaultProjectKey));
        testScope.ActiveSolutionBoundTrackerMock
            .SetupAdd(solutionTracker => solutionTracker.SolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>()).
            Callback(() => 
            {
                isSubscribed.Should().BeFalse();
                isSubscribed = true;
            });
        testScope.SetUpSSEFactoryToReturnSSESession(DefaultProjectKey, factoryMockCallback:() => isSubscribed.Should().BeTrue());

        var _ = testScope.CreateTestSubject();

        testScope.ActiveSolutionBoundTrackerMock.VerifyAdd(tracker => tracker.SolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey), Times.Once);
    }

    [TestMethod]
    public void Ctor_WhenInConnectedModeOnCreation_CreatesSession()
    {
        var testScope = new TestScope(TestScope.CreateConnectedModeBindingConfiguration(DefaultProjectKey));
        testScope.SetUpSSEFactoryToReturnSSESession(DefaultProjectKey);

        var _ = testScope.CreateTestSubject();

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey), Times.Once);
    }

    [TestMethod]
    public void Ctor_WhenSessionIsDisposed_ExitsWithoutThrowing()
    {
        var testScope = new TestScope(TestScope.CreateConnectedModeBindingConfiguration(DefaultProjectKey));
        var sessionMock = testScope.SetUpSSEFactoryToReturnSSESession(
            DefaultProjectKey,
            pumpAllSetup => pumpAllSetup.ThrowsAsync(new ObjectDisposedException(string.Empty)));
        
        var act = () => testScope.CreateTestSubject();

        act.Should().NotThrow();
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey), Times.Once);
        sessionMock.Verify(session => session.PumpAllAsync(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromStandaloneToConnected_CreatesSessionAndLaunchesIt()
    {
        var testScope = new TestScope();
        var _ = testScope.CreateTestSubject();
        testScope.RaiseInStandaloneModeEvent();
        var sessionMock = testScope.SetUpSSEFactoryToReturnSSESession(DefaultProjectKey);

        testScope.RaiseInConnectedModeEvent(DefaultProjectKey);

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey), Times.Once);
        sessionMock.Verify(session => session.PumpAllAsync(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromConnectedToStandalone_DisposesPreviousSession()
    {
        var testScope = new TestScope();
        var _ = testScope.CreateTestSubject();
        var sessionMock = testScope.SetUpSSEFactoryToReturnSSESession(DefaultProjectKey);
        sessionMock.Setup(session => session.Dispose());
        testScope.RaiseInConnectedModeEvent(DefaultProjectKey);
        testScope.SSESessionFactoryMock.Invocations.Clear();

        testScope.RaiseInStandaloneModeEvent();

        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(It.IsAny<string>()), Times.Never);
        sessionMock.Verify(session => session.Dispose(), Times.Once);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenChangesFromConnectedToConnected_CancelsSessionAndStartsNewOne()
    {
        var projectKey1 = "proj1";
        var projectKey2 = "proj2";
        var testScope = new TestScope();
        var _ = testScope.CreateTestSubject();
        var sessionMock1 = testScope.SetUpSSEFactoryToReturnSSESession(projectKey1);
        sessionMock1.Setup(session => session.Dispose());
        testScope.RaiseInConnectedModeEvent(projectKey1);
        var sessionMock2 = testScope.SetUpSSEFactoryToReturnSSESession(projectKey2);

        testScope.RaiseInConnectedModeEvent(projectKey2);

        sessionMock1.Verify(session => session.Dispose(), Times.Once);
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(projectKey2), Times.Once);
        sessionMock2.Verify(session => session.Dispose(), Times.Never);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenAttemptsToConnectToAlreadyConnectedProject_KeepsPreviousSessionAlive()
    {
        var connectedModeBindingConfiguration = TestScope.CreateConnectedModeBindingConfiguration(DefaultProjectKey);
        var testScope = new TestScope(connectedModeBindingConfiguration);
        var sessionMock1 = testScope.SetUpSSEFactoryToReturnSSESession(connectedModeBindingConfiguration.Project.ProjectKey);
        var _ = testScope.CreateTestSubject();
        testScope.SSESessionFactoryMock.Invocations.Clear();

        testScope.RaiseSolutionBindingEvent(connectedModeBindingConfiguration);

        sessionMock1.Verify(session => session.Dispose(), Times.Never);
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(connectedModeBindingConfiguration.Project.ProjectKey), Times.Never);
    }

    [TestMethod]
    public void OnSolutionChanged_WhenSessionIsDisposed_ExitsWithoutThrowing()
    {
        var testScope = new TestScope();
        var _ = testScope.CreateTestSubject();
        var sessionMock = testScope.SetUpSSEFactoryToReturnSSESession(
            DefaultProjectKey, 
            pumpAllSetup => pumpAllSetup.ThrowsAsync(new ObjectDisposedException(string.Empty)));

        var act = () => testScope.RaiseInConnectedModeEvent(DefaultProjectKey);

        act.Should().NotThrow();
        testScope.SSESessionFactoryMock.Verify(factory => factory.Create(DefaultProjectKey), Times.Once);
        sessionMock.Verify(session => session.PumpAllAsync(), Times.Once);
    }


    [TestMethod]
    public void Dispose_WhenInConnectedMode_CorrectAndIdempotent()
    {
        var testScope = new TestScope();
        var testSubject = testScope.CreateTestSubject();
        var sseSession = testScope.SetUpSSEFactoryToReturnSSESession(DefaultProjectKey);
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

            ActiveSolutionBoundTrackerMock
                .InSequence(callOrder)
                .SetupGet(tracker => tracker.CurrentConfiguration)
                .Returns(initialBindingState ?? BindingConfiguration.Standalone);
        }

        public Mock<IActiveSolutionBoundTracker> ActiveSolutionBoundTrackerMock { get; }
        public Mock<ISSESessionFactory> SSESessionFactoryMock { get; }

        public SSESessionManager CreateTestSubject()
        {
            return new SSESessionManager(
                ActiveSolutionBoundTrackerMock.Object,
                SSESessionFactoryMock.Object);
        }

        public static BindingConfiguration CreateConnectedModeBindingConfiguration(string projectKey)
        {
            var randomString = Guid.NewGuid().ToString();
            var bindingConfiguration = new BindingConfiguration(
                new BoundSonarQubeProject(new Uri("http://localhost"), projectKey, randomString),
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

        public Mock<ISSESession> SetUpSSEFactoryToReturnSSESession(string projectKey, Action<ISetup<ISSESession, Task>> sseSessionPumpAllAsyncSetupOverride = null, Action factoryMockCallback = null)
        {
            var sseSessionMock = mockRepository.Create<ISSESession>();
            SSESessionFactoryMock.InSequence(callOrder)
                .Setup(sessionFactory => sessionFactory.Create(projectKey))
                .Returns(sseSessionMock.Object)
                .Callback(() => factoryMockCallback?.Invoke());

            var pumpAllSetupBuilder = sseSessionMock.InSequence(callOrder).Setup(session => session.PumpAllAsync());

            sseSessionPumpAllAsyncSetupOverride ??= pumpAllSetup => pumpAllSetup.Returns(Task.CompletedTask);
            sseSessionPumpAllAsyncSetupOverride(pumpAllSetupBuilder);

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

        public void RaiseSolutionBindingEvent(BindingConfiguration bindingConfiguration)
        {
            ActiveSolutionBoundTrackerMock.Raise(tracker => tracker.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(bindingConfiguration));
        }
    }
}
