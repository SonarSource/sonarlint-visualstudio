//-----------------------------------------------------------------------
// <copyright file="ConfigurableTelemetryLogger.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableTelemetryLogger : ITelemetryLogger
    {
        private readonly List<TelemetryEvent> events = new List<TelemetryEvent>();

        #region IVsTelemetryLogger
        void ITelemetryLogger.ReportEvent(TelemetryEvent telemetryEvent)
        {
            this.events.Add(telemetryEvent);
        }
        #endregion

        #region Helpers
        public void Reset()
        {
            this.events.Clear();
        }

        public void AssertSingleEventWasWritten(TelemetryEvent expected)
        {
            Assert.AreEqual(1, this.events.Count, "Unexpected events: {0}", string.Join(", ", this.events));
            TelemetryEvent actual = this.events.Single();
            Assert.AreEqual(expected, actual, "Unexpected entry name");
        }

        public void AssertNoEventWasWritten()
        {
            Assert.AreEqual(0, this.events.Count, "Unexpected events: {0}", string.Join(", ", this.events));
        }

        public void DumpAllToOutput()
        {
            this.events.ForEach(e => Debug.WriteLine(e));
        }

        #endregion
    }
}
