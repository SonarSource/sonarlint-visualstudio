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
using System.ComponentModel;
using System.Linq;
using System.Net;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class DaemonAnalyzerTests
    {
        private DummySonarLintDaemon dummyDaemon;
        private DummyDaemonInstaller dummyInstaller;
        private DaemonAnalyzer analyzer;

        [TestInitialize]
        public void TestInitialize()
        {
            dummyDaemon = new DummySonarLintDaemon();
            dummyInstaller = new DummyDaemonInstaller();
            analyzer = new DaemonAnalyzer(dummyDaemon, dummyInstaller);
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
        public void RequestAnalysis_Started_AnalysisRequested()
        {
            // Arrange
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = true;

            // 1. Start
            analyzer.RequestAnalysis("path", "charset", null, null, null);

            // Assert - only RequestAnalysis called
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);

            // 2. Check the event handlers have been unsubscribed
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            // Assert - not other calls
            dummyInstaller.InstallCallCount.Should().Be(0);
            dummyDaemon.StartCallCount.Should().Be(0);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
        }

        [TestMethod]
        public void RequestAnalysis_NotStarted_StartThenRequestAnalysis()
        {
            // Arrange
            dummyInstaller.IsInstalledReturnValue = true;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", null, null, null);

            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(0); // should be waiting for the daemon to be ready

            // 2. Simulate daemon being ready
            dummyDaemon.SimulateDaemonReady(null);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);

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
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallThenStartThenRequestAnalysis()
        {
            // Arrange
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", null, null, null);

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

            // 4. Check the event handlers have been unsubscribed
            dummyDaemon.SimulateDaemonReady(null);
            dummyInstaller.SimulateInstallFinished(null);

            // Sanity check of all of the call counts
            dummyInstaller.InstallCallCount.Should().Be(1);
            dummyDaemon.StartCallCount.Should().Be(1);
            dummyDaemon.RequestAnalysisCallCount.Should().Be(1);
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_ErrorOnInstall_StartNotCalled()
        {
            // Arrange
            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", null, null, null);

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
        }

        [TestMethod]
        public void RequestAnalysis_NotInstalled_InstallCancelled_StartNotCalled()
        {
            // Arrange
            var dummyDaemon = new DummySonarLintDaemon();
            var dummyInstaller = new DummyDaemonInstaller();
            var analyzer = new DaemonAnalyzer(dummyDaemon, dummyInstaller);

            dummyInstaller.IsInstalledReturnValue = false;
            dummyDaemon.IsRunning = false;

            // 1. Make the request
            analyzer.RequestAnalysis("path", "charset", null, null, null);

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
        }

        private class DummySonarLintDaemon : ISonarLintDaemon
        {
            #region Test helper methods

            public IEnumerable<SonarLanguage> SupportedLanguages { get; set; }

            public int StartCallCount { get; private set; }
            public int RequestAnalysisCallCount { get; private set; }

            public void SimulateDaemonReady(EventArgs args)
            {
                this.IsRunning = true;
                this.Ready?.Invoke(this, args);
            }

            #endregion

            #region ISonarLintDaemon methods

            public bool IsRunning { get; set; } /* publicly settable for testing */

            public event EventHandler<EventArgs> Ready;

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public bool IsAnalysisSupported(IEnumerable<SonarLanguage> languages)
            {
                return SupportedLanguages.Intersect(languages).Any();
            }

            public void RequestAnalysis(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
            {
                RequestAnalysisCallCount++;
            }

            public void Start()
            {
                StartCallCount++;
            }

            public void Stop()
            {
                throw new NotImplementedException();
            }

            #endregion ISonarLintDaemon methods
        }

        private class DummyDaemonInstaller : IDaemonInstaller
            {
                #region Test helpers

                public int InstallCallCount { get; private set; }

                public bool IsInstalledReturnValue { get; set; }

                public void SimulateInstallFinished(AsyncCompletedEventArgs args)
                {
                    this.IsInstalledReturnValue = true;
                    this.DownloadCompleted?.Invoke(this, args);
                }

                #endregion

                #region IDaemonInstaller methods

                public bool InstallInProgress { get; set; } /* publicly settable for testing */

                public string InstallationPath => throw new NotImplementedException();

                public string DaemonVersion => throw new NotImplementedException();

                public event DownloadProgressChangedEventHandler DownloadProgressChanged;
                public event AsyncCompletedEventHandler DownloadCompleted;

                public void Install()
                {
                    InstallCallCount++;
                }

                public bool IsInstalled()
                {
                    return IsInstalledReturnValue;
                }

                #endregion
            }
    }
}
