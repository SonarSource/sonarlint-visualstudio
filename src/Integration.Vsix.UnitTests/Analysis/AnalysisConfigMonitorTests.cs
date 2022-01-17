/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalysisConfigMonitorTests
    {
        [TestMethod]
        [DataRow(SonarLintMode.Standalone)]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void WhenUserSettingsChange_AnalysisIsRequested(SonarLintMode bindingMode)
        {
            var builder = new TestEnvironmentBuilder(bindingMode);

            builder.SimulateUserSettingsChanged();

            // Should always re-analyse
            builder.AssertAnalysisIsRequested();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsChanged);

            if (bindingMode == SonarLintMode.Standalone)
            {
                builder.Logger.AssertOutputStringDoesNotExist(AnalysisStrings.ConfigMonitor_UserSettingsIgnoredForConnectedModeLanguages);
            }
            else
            {
                builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsIgnoredForConnectedModeLanguages);
            }
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone)]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void WhenUserSettingsChange_HasSubscribersToConfigChangedEvent_SubscribersNotified(SonarLintMode bindingMode)
        {
            var builder = new TestEnvironmentBuilder(bindingMode);
            var eventHandler = new Mock<EventHandler>();
            builder.TestSubject.ConfigChanged += eventHandler.Object;

            builder.SimulateUserSettingsChanged();

            eventHandler.Verify(x=> x(builder.TestSubject, EventArgs.Empty), Times.Once);
        }

        [TestMethod]
        public void WhenSuppressionsUpdated_AnalysisIsRequested()
        {
            var builder = new TestEnvironmentBuilder(SonarLintMode.Connected);

            builder.SimulateSuppressionsUpdated();

            builder.AssertAnalysisIsRequested();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_SuppressionsUpdated);
        }

        [TestMethod]
        public void WhenSuppressionsUpdated_HasSubscribersToConfigChangedEvent_SubscribersNotified()
        {
            var builder = new TestEnvironmentBuilder(SonarLintMode.Connected);
            var eventHandler = new Mock<EventHandler>();
            builder.TestSubject.ConfigChanged += eventHandler.Object;

            builder.SimulateSuppressionsUpdated();

            eventHandler.Verify(x => x(builder.TestSubject, EventArgs.Empty), Times.Once);
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
                    CurrentConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), bindingMode, null)
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
                suppressedIssuesMonitorMock.Raise(x=> x.SuppressionsUpdateRequested += null, EventArgs.Empty);
            }

            public void AssertAnalysisIsRequested()
            {
                analysisRequesterMock.Verify(x => x.RequestAnalysis((IAnalyzerOptions)null, Array.Empty<string>()), Times.Once);
            }

            public void AssertAnalysisIsNotRequested()
            {
                analysisRequesterMock.Invocations.Count.Should().Be(0);
            }
        }
    }
}
