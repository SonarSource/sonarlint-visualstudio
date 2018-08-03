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
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryHelper_CreatePayload
    {
        [TestMethod]
        public void CreatePayload_InvalidArg_Throws()
        {
            Action action = () => TelemetryHelper.CreatePayload(null, DateTimeOffset.Now, BindingConfiguration.Standalone);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("telemetryData");

            action = () => TelemetryHelper.CreatePayload(new TelemetryData(), DateTimeOffset.Now, null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingConfiguration");
        }

        [TestMethod]
        public void CreatePayload_Creates_Payload_Using_SonarCloud()
        {
            // Arrange
            var now = new DateTime(2017, 7, 25, 0, 0, 0, DateTimeKind.Local).AddHours(2);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.AddDays(-10),
                IsAnonymousDataShared = true,
                NumberOfDaysOfUse = 5
            };

            var binding = CreateConfiguration(SonarLintMode.Connected, "https://sonarcloud.io");

            VisualStudioHelpers.VisualStudioVersion = "1.2.3.4";

            // Act
            var result = TelemetryHelper.CreatePayload(
                telemetryData,
                new DateTimeOffset(now),
                binding);

            // Assert
            result.IsUsingConnectedMode.Should().BeTrue();
            result.IsUsingSonarCloud.Should().BeTrue();
            result.NumberOfDaysOfUse.Should().Be(5);
            result.NumberOfDaysSinceInstallation.Should().Be(10);
            result.SonarLintProduct.Should().Be("SonarLint Visual Studio");
            result.SonarLintVersion.Should().Be(
                typeof(TelemetryData).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            result.VisualStudioVersion.Should().Be("1.2.3.4");
            result.InstallDate.Should().Be(new DateTimeOffset(now.AddDays(-10)));
            result.SystemDate.Should().Be(new DateTimeOffset(now));
        }

        [TestMethod]
        public void CreatePayload_Connected_But_Not_Using_SonarCloud()
        {
            // Arrange
            var now = new DateTime(2017, 7, 25);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.Subtract(new TimeSpan(10, 0, 0))
            };

            var binding = CreateConfiguration(SonarLintMode.Connected, "http://localhost");

            // Act
            var result = TelemetryHelper.CreatePayload(telemetryData, now, binding);

            // Assert
            result.IsUsingConnectedMode.Should().BeTrue();
            result.IsUsingSonarCloud.Should().BeFalse();
        }

        [TestMethod]
        public void CreatePayload_NotConnected()
        {
            // Arrange
            var now = new DateTime(2017, 7, 25);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.Subtract(new TimeSpan(10, 0, 0))
            };

            // Act
            var result = TelemetryHelper.CreatePayload(telemetryData, now, BindingConfiguration.Standalone);

            // Assert
            result.IsUsingConnectedMode.Should().BeFalse();
            result.IsUsingSonarCloud.Should().BeFalse();
        }

        [TestMethod]
        public void CreatePayload_NumberOfDaysSinceInstallation_On_InstallationDate()
        {
            var now = new DateTime(2017, 7, 25);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.Subtract(new TimeSpan(23, 59, 59)) // Less than a day
            };

            var binding = CreateConfiguration(SonarLintMode.LegacyConnected, "http://localhost");

            var result = TelemetryHelper.CreatePayload(telemetryData, now, binding);

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

            var binding = CreateConfiguration(SonarLintMode.Connected, "http://localhost");

            var result = TelemetryHelper.CreatePayload(telemetryData, now, binding);

            result.NumberOfDaysSinceInstallation.Should().Be(1);
        }

        [TestMethod]
        public void IsSonarCloud_InvalidUri_Null()
        {
            TelemetryHelper.IsSonarCloud(null).Should().BeFalse();
        }

        [TestMethod]
        public void IsSonarCloud_InvalidUri_Relative()
        {
            UriBuilder builder = new UriBuilder();
            builder.Scheme = "file";
            builder.Path = "..\\..\\foo\\file.txt";
            
            TelemetryHelper.IsSonarCloud(builder.Uri).Should().BeFalse();
        }

        [TestMethod]
        public void IsSonarCloud_Valid_NotSonarCloud()
        {
            CheckIsNotSonarCloud("http://localhost:9000");
            CheckIsNotSonarCloud("https://myserver/sonarcloud");
            CheckIsNotSonarCloud("http://sonarcloud.io/foo"); // not https
            CheckIsNotSonarCloud("https://sonarcloud.ioX/foo");
        }

        [TestMethod]
        public void IsSonarCloud_Valid_Matches_SonarCloud()
        {
            CheckIsSonarCloud("https://sonarcloud.io");
            CheckIsSonarCloud("https://SONARCLOUD.io");
            CheckIsSonarCloud("https://sonarcloud.io/");
            CheckIsSonarCloud("https://SONARCLOUD.io/");

            CheckIsSonarCloud("https://www.sonarcloud.io");
            CheckIsSonarCloud("https://WWW.SONARCLOUD.io");
            CheckIsSonarCloud("https://www.sonarcloud.io/");
            CheckIsSonarCloud("https://www.SONARCLOUD.io/");
        }

        private static BindingConfiguration CreateConfiguration(SonarLintMode mode, string serverUri)
        {
            var project = new BoundSonarQubeProject(new Uri(serverUri), "dummy.project.key");
            return BindingConfiguration.CreateBoundConfiguration(project, mode == SonarLintMode.LegacyConnected);
        }

        private static void CheckIsNotSonarCloud(string uri)
        {
            TelemetryHelper.IsSonarCloud(new Uri(uri)).Should().BeFalse();
        }

        private static void CheckIsSonarCloud(string uri)
        {
            TelemetryHelper.IsSonarCloud(new Uri(uri)).Should().BeTrue();
        }
    }
}
