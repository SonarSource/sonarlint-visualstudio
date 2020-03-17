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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalysisConfigMonitorTests
    {
        [TestMethod]
        [DataRow(SonarLintMode.Standalone, true)]
        [DataRow(SonarLintMode.Connected, false)]
        [DataRow(SonarLintMode.LegacyConnected, false)]
        public void WhenUserSettingsChange(SonarLintMode bindingMode, bool shouldAnalysisBeRequested)
        {
            var builder = new TestEnvironmentBuilder(bindingMode);

            builder.SimulateUserSettingsChanged();

            if (shouldAnalysisBeRequested)
            {
                builder.AssertAnalysisIsRequested();
                builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
            }
            else
            {
                builder.AssertAnalysisIsNotRequested();
                builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_IgnoringUserSettingsChanged);
            }
        }

        [TestMethod]
        public void WhenSupressionsUpdated_AnalysisIsRequested()
        {
            var builder = new TestEnvironmentBuilder(SonarLintMode.Connected);

            builder.SimulateSuppressionsUpdated();

            builder.AssertAnalysisIsRequested();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_BindingUpdated);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone)]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void WhenDisposed_EventsAreIgnored(SonarLintMode bindingMode)
        {
            var builder = new TestEnvironmentBuilder(bindingMode);

            // Act
            builder.TestSubject.Dispose();

            // Raise events and check they are ignored
            builder.SimulateSuppressionsUpdated();
            builder.SimulateUserSettingsChanged();

            builder.AssertAnalysisIsNotRequested();
        }

        private class TestEnvironmentBuilder
        {
            private readonly Mock<IAnalysisRequester> analysisRequesterMock;
            private readonly Mock<IUserSettingsProvider> userSettingsProviderMock;
            private readonly Mock<ISuppressedIssuesMonitor> suppressedIssuesMonitorMock;

            public TestEnvironmentBuilder(SonarLintMode bindingMode)
            {
                analysisRequesterMock = new Mock<IAnalysisRequester>();
                userSettingsProviderMock = new Mock<IUserSettingsProvider>();
                suppressedIssuesMonitorMock = new Mock<ISuppressedIssuesMonitor>();

                var solutionBoundTracker = new ConfigurableActiveSolutionBoundTracker
                {
                    CurrentConfiguration = new BindingConfiguration(new Persistence.BoundSonarQubeProject(), bindingMode)
                };

                Logger = new TestLogger();

                TestSubject = new AnalysisConfigMonitor(analysisRequesterMock.Object,
                    userSettingsProviderMock.Object, solutionBoundTracker, suppressedIssuesMonitorMock.Object, Logger);
            }

            public TestLogger Logger { get; }

            public AnalysisConfigMonitor TestSubject { get; }

            public void SimulateUserSettingsChanged()
            {
                userSettingsProviderMock.Raise(x => x.SettingsChanged += null, EventArgs.Empty);
            }

            public void SimulateSuppressionsUpdated()
            {
                suppressedIssuesMonitorMock.Raise(x=> x.SuppressionsUpdated += null, EventArgs.Empty);
            }

            public void AssertAnalysisIsRequested()
            {
                analysisRequesterMock.Verify(x => x.RequestAnalysis(), Times.Once);
            }

            public void AssertAnalysisIsNotRequested()
            {
                analysisRequesterMock.Verify(x => x.RequestAnalysis(), Times.Never);
            }
        }
    }
}
