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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;

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

        #endregion IVsTelemetryLogger

        #region Helpers

        public void Reset()
        {
            this.events.Clear();
        }

        public void AssertSingleEventWasWritten(TelemetryEvent expected)
        {
            this.events.Should().HaveCount(1);
            TelemetryEvent actual = this.events.Single();
            actual.Should().Be(expected, "Unexpected entry name");
        }

        public void AssertNoEventWasWritten()
        {
            this.events.Should().BeEmpty();
        }

        public void DumpAllToOutput()
        {
            this.events.ForEach(e => Debug.WriteLine(e));
        }

        #endregion Helpers
    }
}