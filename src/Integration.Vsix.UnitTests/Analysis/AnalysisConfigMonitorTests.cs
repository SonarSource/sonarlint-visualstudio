﻿/*
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.UnitTests;
using static SonarLint.VisualStudio.Integration.UnitTests.NoOpThreadHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalysisConfigMonitorTests
    {
        [TestMethod]
        public void WhenUserSettingsChange_AnalysisIsRequested()
        {
            var builder = new TestEnvironmentBuilder();

            builder.SimulateUserSettingsChanged();

            // Should re-analyse
            builder.AssertAnalysisIsRequested();
            builder.AssertSwitchedToBackgroundThread();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
        }

        [TestMethod]
        public void WhenUserSettingsChange_HasSubscribersToConfigChangedEvent_SubscribersNotified()
        {
            var builder = new TestEnvironmentBuilder();
            var eventHandler = new Mock<EventHandler>();
            builder.TestSubject.ConfigChanged += eventHandler.Object;

            builder.SimulateUserSettingsChanged();
            builder.AssertSwitchedToBackgroundThread();

            eventHandler.Verify(x=> x(builder.TestSubject, EventArgs.Empty), Times.Once);
            
        }

        [TestMethod]
        public void WhenSuppressionsUpdated_AnalysisIsRequested()
        {
            var builder = new TestEnvironmentBuilder();

            builder.SimulateSuppressionsUpdated();

            builder.AssertSwitchedToBackgroundThread();
            builder.AssertAnalysisIsRequested();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_SuppressionsUpdated);
        }

        [TestMethod]
        public void WhenSuppressionsUpdated_HasSubscribersToConfigChangedEvent_SubscribersNotified()
        {
            var builder = new TestEnvironmentBuilder();
            var eventHandler = new Mock<EventHandler>();
            builder.TestSubject.ConfigChanged += eventHandler.Object;

            builder.SimulateSuppressionsUpdated();            
            
            builder.AssertSwitchedToBackgroundThread();
            eventHandler.Verify(x => x(builder.TestSubject, EventArgs.Empty), Times.Once);
        }

        [TestMethod]
        public void WhenDisposed_EventsAreIgnored()
        {
            var builder = new TestEnvironmentBuilder();

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
            private readonly Mock<IThreadHandling> threadHandling;


            public TestEnvironmentBuilder()
            {
                analysisRequesterMock = new Mock<IAnalysisRequester>();
                userSettingsProviderMock = new Mock<IUserSettingsProvider>();
                suppressedIssuesMonitorMock = new Mock<ISuppressedIssuesMonitor>();
                threadHandling = new Mock<IThreadHandling>();
                
                threadHandling.Setup(th => th.SwitchToBackgroundThread()).Returns(new NoOpAwaitable());

                Logger = new TestLogger();

                TestSubject = new AnalysisConfigMonitor(analysisRequesterMock.Object,
                    userSettingsProviderMock.Object, suppressedIssuesMonitorMock.Object, Logger, threadHandling.Object);
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

            public void AssertSwitchedToBackgroundThread()
            {
                threadHandling.Verify(x => x.SwitchToBackgroundThread(), Times.Once);
            }
        }
    }
}
