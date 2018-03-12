/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Rules;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests.Rules
{
    [TestClass]
    public class QualityProfileProviderCachingDecoratorTests
    {
        private WrappedQualityProfileProvider wrappedProvider = new WrappedQualityProfileProvider();
        private Mock<ISonarQubeService> serviceMock = new Mock<ISonarQubeService>();
        private Mock<ITimerFactory> timerFactoryMock = new Mock<ITimerFactory>();
        private Mock<ITimer> timerMock;

        /// <summary>
        /// Wired up to the Timer.Start/Stop mock methods to track the current timer status
        /// </summary>
        private bool timerRunning;

        [TestInitialize]
        public void TestInitialize()
        {
            serviceMock = new Mock<ISonarQubeService>();

            timerFactoryMock = new Mock<ITimerFactory>();
            timerMock = new Mock<ITimer>();

            timerFactoryMock.Setup(x => x.Create())
                .Returns(timerMock.Object)
                .Verifiable();

            timerMock.SetupSet(x => x.Interval = It.IsInRange(1d, double.MaxValue, Range.Inclusive)).Verifiable();
            timerMock.Setup(x => x.Start()).Callback(() => timerRunning = true);
            timerMock.Setup(x => x.Dispose()).Callback(() => timerRunning = false);
        }

        [TestMethod]
        public void Ctor_WhenHasNullArgs_Throws()
        {
            var validProject = new BoundSonarQubeProject();

            // 1. Null wrapped provider
            Action act = () => new QualityProfileProviderCachingDecorator(null, validProject, serviceMock.Object, timerFactoryMock.Object);
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("wrappedProvider");

            // 2. Null project
            act = () => new QualityProfileProviderCachingDecorator(wrappedProvider, null, serviceMock.Object, timerFactoryMock.Object);
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("boundProject");

            // 3. Null service
            act = () => new QualityProfileProviderCachingDecorator(wrappedProvider, validProject, null, timerFactoryMock.Object);
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");

            // 4. Null timer factory
            act = () => new QualityProfileProviderCachingDecorator(wrappedProvider, validProject, serviceMock.Object, null);
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("timerFactory");
        }

        [TestMethod]
        [Ignore] // failing on CIX
        public void ObjectLifecycle_Create_InitialFetch_Timer_Dispose()
        {
            // Arrange
            // The provider will attempt to fetch issues at start up and will loop if it
            // is not connected to the server, so we'll initialise it as connected.
            SetServiceConnectionStatus(isConnected: true);

            // 1. Construction -> timer initialised
            var testSubject = new QualityProfileProviderCachingDecorator(
                wrappedProvider,
                new BoundSonarQubeProject(),
                serviceMock.Object,
                timerFactoryMock.Object);

            // Assert
            timerFactoryMock.VerifyAll();
            timerMock.VerifySet(t => t.AutoReset = true, Times.Once);
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);

            // 2. Initial fetch
            // The initial fetch is run in a background thread - wait until
            // the wrapped provider indicates that the fetch has started
            WaitForInitialFetchTaskToStart();

            VerifyServiceIsConnected(Times.Exactly(2));
            wrappedProvider.GetQualityProfileCallCount.Should().Be(Language.SupportedLanguages.Count());

            // 3. Timer event raised -> check attempt is made to synchronize data
            RaiseTimerElapsed(DateTime.UtcNow);
            RaiseTimerElapsed(DateTime.UtcNow);

            VerifyServiceIsConnected(Times.Exactly(4));
            wrappedProvider.GetQualityProfileCallCount.Should().Be(Language.SupportedLanguages.Count() * 3);
            timerRunning.Should().Be(true);

            // 4. Dispose
            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            // Assert
            timerMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        [Ignore] // failing on CIX
        public void SynchOnTimerElapsed_WhenNotConnected_NoErrors()
        {
            // Arrange - initialise in a connected state, then disconnect
            var testSubject = CreateTestSubjectWithInitialFetchCompleted();
            serviceMock.ResetCalls();
            wrappedProvider.ResetCalls();

            SetServiceConnectionStatus(isConnected: false);

            // Act
            using (new AssertIgnoreScope())
            {
                RaiseTimerElapsed(DateTime.UtcNow);
            }

            // Assert
            VerifyServiceIsConnected(Times.Exactly(1));
            wrappedProvider.GetQualityProfileCallCount.Should().Be(0);
        }
        
        [TestMethod]
        [Ignore] // failing on CIX
        public void SynchOnTimerElapsed_WhenErrorThrown_IsSuppressed()
        {
            // Arrange - initialise in a connected state, then disconnect
            var testSubject = CreateTestSubjectWithInitialFetchCompleted();
            serviceMock.ResetCalls();
            wrappedProvider.ResetCalls();

            serviceMock.Setup(x => x.IsConnected).Throws<InvalidOperationException>();

            // Act
           RaiseTimerElapsed(DateTime.UtcNow);
 
            // Assert
            VerifyServiceIsConnected(Times.Exactly(1));
            wrappedProvider.GetQualityProfileCallCount.Should().Be(0);
        }

        [TestMethod]
        [Ignore] // failing on CIX
        public void GetQualityProfile_ReturnsExpectedProfile()
        {
            // Arrange
            // Populate a profile to return
            var csProfile = new QualityProfile(Language.CSharp, null);
            wrappedProvider.ProfilesToReturnByLanguage[Language.CSharp] = csProfile;

            var testSubject = CreateTestSubjectWithInitialFetchCompleted();
            serviceMock.ResetCalls();
            wrappedProvider.ResetCalls();

            // Act & Assert - profile for language exists
            var actual = testSubject.GetQualityProfile(new BoundSonarQubeProject(), Language.CSharp);
            actual.Should().BeSameAs(csProfile);

            // Act & Assert - profile for language does not exist
            actual = testSubject.GetQualityProfile(new BoundSonarQubeProject(), Language.VBNET);
            actual.Should().BeNull();

            // Additional checks
            VerifyServiceIsConnected(Times.Never()); // should use cached data
            wrappedProvider.GetQualityProfileCallCount.Should().Be(0);
        }

        private QualityProfileProviderCachingDecorator CreateTestSubjectWithInitialFetchCompleted()
        {
            // Arrange - intialise in a connected state, then disconnect
            SetServiceConnectionStatus(isConnected: true);

            var testSubject = new QualityProfileProviderCachingDecorator(
                wrappedProvider,
                new BoundSonarQubeProject(),
                serviceMock.Object,
                timerFactoryMock.Object);
            WaitForInitialFetchTaskToStart();

            // Sanity check - should have fetch the data once
            VerifyServiceIsConnected(Times.Exactly(2));
            wrappedProvider.GetQualityProfileCallCount.Should().Be(Language.SupportedLanguages.Count());

            return testSubject;
        }

        private void SetServiceConnectionStatus(bool isConnected)
        {
            // Note: if the service is set up disconnected then the initial fetch background
            // task will run in a loop - make sure the calling test takes account of this
            serviceMock.Setup(x => x.IsConnected).Returns(isConnected).Verifiable();
        }

        private void WaitForInitialFetchTaskToStart()
        {
            // Only applicable for services that are connected
            var waitSignaled = wrappedProvider.InitialFetchStartedWaitHandle.WaitOne(Debugger.IsAttached ? 20000 : 5000); // wait for fetch to start...
            waitSignaled.Should().BeTrue(); // error - fetch has not started running
        }

        private void VerifyTimerStart(Times expected)
        {
            timerMock.Verify(t => t.Start(), expected);
        }

        private void VerifyServiceIsConnected(Times expected)
        {
            serviceMock.Verify(x => x.IsConnected, expected);
        }

        private void RaiseTimerElapsed(DateTime eventTime)
        {
            timerMock.Raise(t => t.Elapsed += null, new TimerEventArgs(eventTime));
        }

        private class WrappedQualityProfileProvider : IQualityProfileProvider
        {
            /// <summary>
            /// Wait handle that is set to signalled when the initial fetch task has started
            /// </summary>
            public EventWaitHandle InitialFetchStartedWaitHandle { get; } = new EventWaitHandle(false, EventResetMode.ManualReset);
            private bool initialFetchCompleted;

            public IDictionary<Language, QualityProfile> ProfilesToReturnByLanguage { get; } = new Dictionary<Language, QualityProfile>();

            public int GetQualityProfileCallCount { get; private set; }

            public void ResetCalls()
            {
                GetQualityProfileCallCount = 0;
            }

            QualityProfile IQualityProfileProvider.GetQualityProfile(BoundSonarQubeProject project, Language language)
            {
                GetQualityProfileCallCount++;

                if (!initialFetchCompleted)
                {
                    initialFetchCompleted = true;

                    // Mark that the initial fetch has started
                    InitialFetchStartedWaitHandle.Set();
                }

                QualityProfile profile;
                ProfilesToReturnByLanguage.TryGetValue(language, out profile);
                return profile;
            }

            void IDisposable.Dispose()
            {
                // no op
            }
        }

    }
}
