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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry
{
    [TestClass]
    public class TelemetryLoggerTests
    {
        private static readonly Guid ExpectedCommandSetIdentifier = new Guid("{DB0701CC-1E44-41F7-97D6-29B160A70BCB}");

        private ConfigurableServiceProvider serviceProvider;
        private DTEMock dte;
        private ConfigurableVsOutputWindowPane outputPane;
        private TelemetryLogger testSubject;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(EnvDTE.DTE), this.dte);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.testSubject = new TelemetryLogger(this.serviceProvider);
        }

        [TestMethod]
        public void TelemetryLogger_ReportEvent()
        {
            // Arrange
            Dictionary<TelemetryEvent, int> discoveredMap = new Dictionary<TelemetryEvent, int>();

            foreach (TelemetryEvent evnt in Enum.GetValues(typeof(TelemetryEvent)).OfType<TelemetryEvent>())
            {
                this.dte.Commands.RaiseAction = (commandGroup, commandId) =>
                {
                    commandGroup.Should().Be(ExpectedCommandSetIdentifier, "Unexpected command group");
                    discoveredMap[evnt] = commandId;
                };

                // Act
                this.testSubject.ReportEvent(evnt);
            }

            // Assert
            TelemetryEvent[] expectedEvents = Enum.GetValues(typeof(TelemetryEvent)).Cast<TelemetryEvent>().ToArray();
            TelemetryEvent[] actualEvents = discoveredMap.Keys.ToArray();

            CollectionAssert.AreEquivalent(expectedEvents, actualEvents,
                "Expecting each telemetry event to call a unique command - no all telemetry events invoked a command. Missing:{0}, NotExpected:{1}",
                  string.Join(", ", expectedEvents.Except(actualEvents)),
                    string.Join(", ", actualEvents.Except(expectedEvents)));

            int[] expectedIds = Enum.GetValues(typeof(SonarLintSqmCommandIds)).Cast<int>().ToArray();
            int[] actualIds = discoveredMap.Values.ToArray();

            CollectionAssert.AreEquivalent(expectedIds, actualIds,
                "Expecting each telemetry event to call a unique command - some of the commands were not uniquely called. Missing:{0}, NotExpected:{1}",
                string.Join(", ", expectedIds.Except(actualIds).Cast<SonarLintSqmCommandIds>()),
                string.Join(", ", actualIds.Except(expectedIds).Cast<SonarLintSqmCommandIds>()));
        }
    }
}