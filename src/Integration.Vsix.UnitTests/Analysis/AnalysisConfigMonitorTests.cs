/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests
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
        [DataRow(SonarLintMode.Standalone, false)]
        [DataRow(SonarLintMode.Connected, true)]
        [DataRow(SonarLintMode.LegacyConnected, true)]
        public void WhenBindingIsUpdated(SonarLintMode bindingMode, bool shouldAnalysisBeRequested)
        {
            var builder = new TestEnvironmentBuilder(bindingMode);

            builder.SimulateBindingUpdated ();

            if (shouldAnalysisBeRequested)
            {
                builder.AssertAnalysisIsRequested();
                builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_BindingUpdated);
            }
            else
            {
                builder.AssertAnalysisIsNotRequested();
                builder.Logger.AssertNoOutputMessages();
            }
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone, false)]
        [DataRow(SonarLintMode.Connected, true)]
        [DataRow(SonarLintMode.LegacyConnected, true)]
        public void WhenBindingIsChanged(SonarLintMode bindingMode, bool shouldAnalysisBeRequested)
        {
            var builder = new TestEnvironmentBuilder(bindingMode);

            builder.SimulateBindingChanged();

            if (shouldAnalysisBeRequested)
            {
                builder.AssertAnalysisIsRequested();
                builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_SolutionBound);
            }
            else
            {
                builder.AssertAnalysisIsNotRequested();
                builder.Logger.AssertNoOutputMessages();
            }
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
            builder.SimulateBindingChanged();
            builder.SimulateBindingUpdated();
            builder.SimulateUserSettingsChanged();

            builder.AssertAnalysisIsNotRequested();
        }

        private class TestEnvironmentBuilder
        {
            private readonly Mock<IAnalysisRequester> analysisRequesterMock;
            private readonly Mock<IUserSettingsProvider> userSettingsProviderMock;
            private readonly ConfigurableActiveSolutionBoundTracker solutionBoundTracker;

            private readonly SonarLintMode bindingMode;

            public TestEnvironmentBuilder(SonarLintMode bindingMode)
            {
                this.bindingMode = bindingMode;

                analysisRequesterMock = new Mock<IAnalysisRequester>();
                userSettingsProviderMock = new Mock<IUserSettingsProvider>();
                solutionBoundTracker = new ConfigurableActiveSolutionBoundTracker()
                {
                    CurrentConfiguration = new BindingConfiguration(new Persistence.BoundSonarQubeProject(), bindingMode)
                };

                Logger = new TestLogger();

                TestSubject = new AnalysisConfigMonitor(analysisRequesterMock.Object,
                    userSettingsProviderMock.Object, solutionBoundTracker, Logger);
            }

            public TestLogger Logger { get; }

            public AnalysisConfigMonitor TestSubject { get; }

            public void SimulateUserSettingsChanged()
            {
                userSettingsProviderMock.Raise(x => x.SettingsChanged += null, EventArgs.Empty);
            }

            public void SimulateBindingChanged()
            {
                var newConfig = new BindingConfiguration(new Persistence.BoundSonarQubeProject(), bindingMode);
                solutionBoundTracker.SimulateSolutionBindingChanged(new ActiveSolutionBindingEventArgs(newConfig));
            }

            public void SimulateBindingUpdated()
            {
                solutionBoundTracker.SimulateSolutionBindingUpdated();
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
