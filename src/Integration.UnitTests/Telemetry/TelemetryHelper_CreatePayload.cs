/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryHelper_CreatePayload
    {
        [TestMethod]
        public void CreatePayload_Creates_Payload()
        {
            var now = new DateTime(2017, 7, 25);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.AddDays(-10),
                IsAnonymousDataShared = true,
                NumberOfDaysOfUse = 5
            };

            var result = TelemetryHelper.CreatePayload(telemetryData, now, isConnected: true);

            result.IsUsingConnectedMode.Should().BeTrue();
            result.NumberOfDaysOfUse.Should().Be(5);
            result.NumberOfDaysSinceInstallation.Should().Be(10);
            result.SonarLintProduct.Should().Be("SonarLint Visual Studio");
            result.SonarLintVersion.Should().Be(
                typeof(TelemetryData).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);

            // Cannot test result.VisualStudioVersion because it is string.Empty when running tests
        }

        [TestMethod]
        public void CreatePayload_NumberOfDaysSinceInstallation_On_InstallationDate()
        {
            var now = new DateTime(2017, 7, 25);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.Subtract(new TimeSpan(23, 59, 59)) // Less than a day
            };

            var result = TelemetryHelper.CreatePayload(telemetryData, now, isConnected: true);

            result.NumberOfDaysSinceInstallation.Should().Be(0);
        }

        [TestMethod]
        public void CreatePayload_NumberOfDaysSinceInstallation_Day_After_InstallationDate()
        {
            var now = new DateTime(2017, 7, 25);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.AddDays(-1)
            };

            var result = TelemetryHelper.CreatePayload(telemetryData, now, isConnected: true);

            result.NumberOfDaysSinceInstallation.Should().Be(1);
        }
    }
}
