//-----------------------------------------------------------------------
// <copyright file="TelemetryLoggerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Collections.Generic;
using System.Linq;

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
            // Setup
            Dictionary<TelemetryEvent, int> discoveredMap = new Dictionary<TelemetryEvent, int>();

            foreach (TelemetryEvent evnt in Enum.GetValues(typeof(TelemetryEvent)).OfType<TelemetryEvent>())
            {
                this.dte.Commands.RaiseAction = (commandGroup, commandId) =>
                {
                    Assert.AreEqual(ExpectedCommandSetIdentifier, commandGroup, "Unexpected command group");
                    discoveredMap[evnt] = commandId;
                };

                // Act
                this.testSubject.ReportEvent(evnt);
            }

            // Verify
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
