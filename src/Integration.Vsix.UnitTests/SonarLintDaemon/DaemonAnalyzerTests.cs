/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class DaemonAnalyzerTests
    {
        private DummySonarLintDaemon dummyDaemon;
        private DummyDaemonInstaller dummyInstaller;
        private DaemonAnalyzer analyzer;
        private Mock<ITelemetryManager> telemetryManagerMock;
        private Mock<IIssueConsumer> consumerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            dummyDaemon = new DummySonarLintDaemon();
            dummyInstaller = new DummyDaemonInstaller();
            telemetryManagerMock = new Mock<ITelemetryManager>();
            analyzer = new DaemonAnalyzer(dummyDaemon, dummyInstaller, telemetryManagerMock.Object);

            consumerMock = new Mock<IIssueConsumer>();
        }

        [TestMethod]
        public void IsSupported_True()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.CFamily };

            // Act
            var result = analyzer.IsAnalysisSupported(new[] { AnalysisLanguage.CFamily });

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsSupported_False()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.CFamily };

            // Act
            var result = analyzer.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript });

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_Started_NotSupported_AnalysisRequested()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.CFamily };
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = true;

            // Act
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            // Assert - analysis not called
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);
            CheckTelemetryManagerCallCount("js", 0);
            CheckFailureOutcomeReportedToConsumer();

            // Sanity check the other call counts
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(0);
        }

        [TestMethod]
        public void RequestAnalysis_Started_AnalysisRequested()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = true;

            // 1. Start
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            // Assert - only RequestAnalysis called
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckNoOutputReportedToConsumer();

            CheckTelemetryManagerCallCount("js", 1);

            CheckEventHandlersUnsubscribed();
            CheckTelemetryManagerTotalCallCount(1);
        }

        [TestMethod]
        public void RequestAnalysis_NotStarted_StartThenRequestAnalysis()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready

            // 2. Simulate daemon being ready
            dummyDaemon.SimulateDaemonReady(null);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckTelemetryManagerCallCount("js", 1);

            // Sanity check of all of the call counts
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);

            // 3. Check the event handlers have been unsubscribed
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            CheckNoOutputReportedToConsumer(); // successfully execution, so should not have called consumer.Finished(...)

            // Call counts should not have changed
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckTelemetryManagerTotalCallCount(1);
        }

        [TestMethod]
        public void RequestAnalysis_NotStarted_StartThenRequestAnalysis_NonCriticalExceptionInMakeRequestIsSuppressed()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = false;

            bool requestAnalysisOpInvoked = false;
            dummyDaemon.RequestAnalysisOperation = () =>
            {
                requestAnalysisOpInvoked = true;
                throw new InvalidOperationException("xxx");
            };

            // 1. Make the request
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready

            // 2. Simulate daemon being ready
            dummyDaemon.SimulateDaemonReady(null);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckFailureOutcomeReportedToConsumer();
            CheckTelemetryManagerCallCount("js", 1);

            requestAnalysisOpInvoked.Should().BeTrue();

            // Sanity check of all of the call counts
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);

            CheckEventHandlersUnsubscribed();
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallThenStartThenRequestAnalysis()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript}, consumerMock.Object, null, CancellationToken.None);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);  // should be waiting for the daemon to be installed
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);

            // 2. Simulate daemon being installed
            dummyInstaller.SimulateInstallFinished(new AsyncCompletedEventArgs(null, false /* cancelled */, null));
            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready    

            // 3. Simulate daemon being ready
            dummyDaemon.SimulateDaemonReady(null);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckTelemetryManagerCallCount("js", 1);

            CheckNoOutputReportedToConsumer(); // successfully execution, so should not have called consumer.Finished(...)

            CheckEventHandlersUnsubscribed();
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallCalled_DaemonAlreadyRunningSoStartNotCalled_RequestAnalysisCalled()
        {
            // Related to https://github.com/SonarSource/sonarlint-visualstudio/issues/999

            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);  // should be waiting for the daemon to be installed
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);

            // 2. Simulate daemon started by another caller, then send the "installation complete" notification
            dummyDaemon.IsRunning = true;
            dummyInstaller.SimulateInstallFinished(new AsyncCompletedEventArgs(null, false /* cancelled */, null));

            // Should not call Start in this case: should just request the analysis
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckNoOutputReportedToConsumer();

            CheckEventHandlersUnsubscribed();
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallCalled_DaemonAlreadyRunning_RequestAnalysisCalled_NonCriticalIsSuppressed()
        {
            // Related to https://github.com/SonarSource/sonarlint-visualstudio/issues/999

            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            bool requestAnalysisOpInvoked = false;
            dummyDaemon.RequestAnalysisOperation = () =>
            {
                requestAnalysisOpInvoked = true;
                throw new InvalidOperationException("xxx");
            };

            // 1. Make the request
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);  // should be waiting for the daemon to be installed
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);

            // 2. Simulate daemon started by another caller, then send the "installation complete" notification
            dummyDaemon.IsRunning = true;
            dummyInstaller.SimulateInstallFinished(new AsyncCompletedEventArgs(null, false /* cancelled */, null));

            // Exception should be suppressed
            requestAnalysisOpInvoked.Should().BeTrue();
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckFailureOutcomeReportedToConsumer();

            CheckEventHandlersUnsubscribed();
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_ErrorOnInstall_StartNotCalled()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);  // should be waiting for the daemon to be installed
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);

            // 2. Simulate daemon being installed
            var args = new AsyncCompletedEventArgs(new InvalidOperationException("XXX"), false /* cancelled */, null);
            dummyInstaller.SimulateInstallFinished(args);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready
            CheckFailureOutcomeReportedToConsumer();

            CheckEventHandlersUnsubscribed();
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallCancelled_StartNotCalled()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { AnalysisLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.Javascript }, consumerMock.Object, null, CancellationToken.None);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);  // should be waiting for the daemon to be installed
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);

            // 2. Simulate daemon being installed
            var args = new AsyncCompletedEventArgs(null, true /* cancelled */ , null);
            dummyInstaller.SimulateInstallFinished(args);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready
            CheckFailureOutcomeReportedToConsumer();

            CheckEventHandlersUnsubscribed();
        }

        private void CheckEventHandlersUnsubscribed()
        {
            // Check that firing the events doesn't result in any more method calls.

            // Store the current counts
            var installCallCount = dummyInstaller.InstallCallCount;
            var startCallCount = dummyDaemon.StartCallCount;
            var requestCallCount = dummyDaemon.RequestAnalysisCallCount;

            // Make the telemetry manager throw if it is called again
            telemetryManagerMock.Setup(x => x.LanguageAnalyzed(It.IsAny<string>())).Throws<InvalidOperationException>();

            // Fire the events
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            // Check the call counts haven't changed
            dummyInstaller.InstallCallCount.Should().Be(installCallCount);
            dummyDaemon.StartCallCount.Should().Be(startCallCount);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(requestCallCount);
        }

        private void CheckTelemetryManagerCallCount(string languageKey, int expectedCallCount)
        {
            telemetryManagerMock.Verify(x => x.LanguageAnalyzed(languageKey), Times.Exactly(expectedCallCount));
        }

        private void CheckTelemetryManagerTotalCallCount(int expectedTotalCallCount)
        {
            telemetryManagerMock.Verify(x => x.LanguageAnalyzed(It.IsAny<string>()), Times.Exactly(expectedTotalCallCount));
        }

        private void CheckFailureOutcomeReportedToConsumer()
        {
            // The daemon class wraps the inner analyzer that actually does the work so the class
            // we are currently testing should never call "consumer.Finished(true)" since it can't
            // ever know that is the case. It should only ever call "consumer.Finished(false)" when
            // there is a problem.
            consumerMock.Verify(x => x.Finished(false), Times.Once);
            consumerMock.Verify(x => x.Finished(true), Times.Never);
        }

        private void CheckNoOutputReportedToConsumer() =>
            // If the dummyDaemon was invoked successfully then the class under test should
            // not called "Finished(true/false)"
            consumerMock.Verify(x => x.Finished(It.IsAny<bool>()), Times.Never);
    }
}
