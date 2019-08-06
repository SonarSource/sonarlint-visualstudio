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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class DaemonAnalyzerTests
    {
        private DummySonarLintDaemon dummyDaemon;
        private DummyDaemonInstaller dummyInstaller;
        private DaemonAnalyzer analyzer;
        private Mock<ITelemetryManager> telemetryManagerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            dummyDaemon = new DummySonarLintDaemon();
            dummyInstaller = new DummyDaemonInstaller();
            telemetryManagerMock = new Mock<ITelemetryManager>();
            analyzer = new DaemonAnalyzer(dummyDaemon, dummyInstaller, telemetryManagerMock.Object);
        }

        [TestMethod]
        public void IsSupported_True()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.CFamily };

            // Act
            var result = analyzer.IsAnalysisSupported(new[] { SonarLanguage.CFamily });

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsSupported_False()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.CFamily };

            // Act
            var result = analyzer.IsAnalysisSupported(new[] { SonarLanguage.Javascript });

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_Started_NotSupported_AnalysisRequested()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.CFamily };
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = true;

            // Act
            analyzer.RequestAnalysis("path", "charset", new[] { SonarLanguage.Javascript }, null, null);

            // Assert - analysis not called
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);
            CheckTelemetryManagerCallCount("js", 0);

            // Sanity check the other call counts
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(0);
        }

        [TestMethod]
        public void RequestAnalysis_Started_AnalysisRequested()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = true;

            // 1. Start
            analyzer.RequestAnalysis("path", "charset", new[] { SonarLanguage.Javascript }, null, null);

            // Assert - only RequestAnalysis called
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckTelemetryManagerCallCount("js", 1);
            
            // 2. Check the event handlers have been unsubscribed
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            // Assert - not other calls
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckTelemetryManagerTotalCallCount(1);
        }

        [TestMethod]
        public void RequestAnalysis_NotStarted_StartThenRequestAnalysis()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", new[] { SonarLanguage.Javascript }, null, null);

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

            // Call counts should not have changed
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckTelemetryManagerTotalCallCount(1);
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallThenStartThenRequestAnalysis()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", new[] { SonarLanguage.Javascript}, null, null);

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

            // 4. Check the event handlers have been unsubscribed
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            // Sanity check of all of the call counts
            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
            CheckTelemetryManagerTotalCallCount(1);
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_ErrorOnInstall_StartNotCalled()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", new[] { SonarLanguage.Javascript }, null, null);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);  // should be waiting for the daemon to be installed
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);

            // 2. Simulate daemon being installed
            var args = new AsyncCompletedEventArgs(new InvalidOperationException("XXX"), false /* cancelled */, null);
            dummyInstaller.SimulateInstallFinished(args);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready

            // 3. Check the event handlers have been unsubscribed
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            // Sanity check of all of the call counts
            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);
            CheckTelemetryManagerTotalCallCount(0);
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallCancelled_StartNotCalled()
        {
            // Arrange
            dummyDaemon.SupportedLanguages = new[] { SonarLanguage.Javascript };
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", new[] { SonarLanguage.Javascript }, null, null);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);  // should be waiting for the daemon to be installed
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);

            // 2. Simulate daemon being installed
            var args = new AsyncCompletedEventArgs(null, true /* cancelled */ , null);
            dummyInstaller.SimulateInstallFinished(args);

            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready

            // 3. Check the event handlers have been unsubscribed
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            // Sanity check of all of the call counts
            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0);
            CheckTelemetryManagerTotalCallCount(0);
        }

        private void CheckTelemetryManagerCallCount(string languageKey, int expectedCallCount)
        {
            telemetryManagerMock.Verify(x => x.LanguageAnalyzed(languageKey), Times.Exactly(expectedCallCount));
        }

        private void CheckTelemetryManagerTotalCallCount(int expectedTotalCallCount)
        {
            telemetryManagerMock.Verify(x => x.LanguageAnalyzed(It.IsAny<string>()), Times.Exactly(expectedTotalCallCount));
        }
    }
}
