/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryHelper_CreatePayload
    {
        [TestMethod]
        public void CreatePayload_InvalidArg_Throws()
        {
            Action action = () => TelemetryHelper.CreatePayload(null, DateTimeOffset.Now, BindingConfiguration.Standalone, null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("telemetryData");

            action = () => TelemetryHelper.CreatePayload(new TelemetryData(), DateTimeOffset.Now, null, null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingConfiguration");
        }

        [TestMethod]
        public void CreatePayload_Creates_Payload_ReturnsCorrectProductAndDates()
        {
            // Arrange
            var now = new DateTime(2017, 7, 25, 0, 0, 0, DateTimeKind.Local).AddHours(2);

            var telemetryData = new TelemetryData
            {
                InstallationDate = now.AddDays(-10),
                IsAnonymousDataShared = true,
                NumberOfDaysOfUse = 5,
                ShowHotspot = new ShowHotspot { NumberOfRequests = 11 },
                TaintVulnerabilities = new TaintVulnerabilities { NumberOfIssuesInvestigatedRemotely = 44, NumberOfIssuesInvestigatedLocally = 55 }
            };

            var binding = CreateConfiguration(SonarLintMode.Connected, "https://sonarcloud.io");

            VisualStudioHelpers.VisualStudioVersion = "1.2.3.4";

            // Act
            var result = TelemetryHelper.CreatePayload(
                telemetryData,
                new DateTimeOffset(now),
                binding,
                null);

            // Assert
            result.NumberOfDaysOfUse.Should().Be(5);
            result.NumberOfDaysSinceInstallation.Should().Be(10);
            result.SonarLintProduct.Should().Be("SonarLint Visual Studio");
            result.SonarLintVersion.Should().Be(
                typeof(TelemetryData).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            result.VisualStudioVersion.Should().Be("1.2.3.4");
            result.InstallDate.Should().Be(new DateTimeOffset(now.AddDays(-10)));
            result.SystemDate.Should().Be(new DateTimeOffset(now));
            result.ShowHotspot.NumberOfRequests.Should().Be(11);
            result.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(44);
            result.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(55);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone, null, false, false, false)]
        [DataRow(SonarLintMode.Connected, "http://localhost", true, false, false)]
        [DataRow(SonarLintMode.Connected, "https://sonarcloud.io/", true, false, true)]
        [DataRow(SonarLintMode.LegacyConnected, "http://anotherlocalhost", true, true, false)]
        [DataRow(SonarLintMode.LegacyConnected, "https://sonarcloud.io/", true, true, true)]
        public void CreatePayload_ReturnsCorrectConnectionData(SonarLintMode mode, string serverUrl,
            bool expectedIsConnected, bool expectedIsLegacyConnected, bool expectedIsSonarCloud)
        {
            // Arrange
            var now = new DateTime(2017, 7, 25);
            var telemetryData = new TelemetryData
            {
                InstallationDate = now.Subtract(new TimeSpan(10, 0, 0))
            };

            var binding = CreateConfiguration(mode, serverUrl);

            // Act
            var result = TelemetryHelper.CreatePayload(telemetryData, now, binding, null);

            // Assert
            result.IsUsingConnectedMode.Should().Be(expectedIsConnected);
            result.IsUsingLegacyConnectedMode.Should().Be(expectedIsLegacyConnected);
            result.IsUsingSonarCloud.Should().Be(expectedIsSonarCloud);
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

            var result = TelemetryHelper.CreatePayload(telemetryData, now, binding, null);

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

            var result = TelemetryHelper.CreatePayload(telemetryData, now, binding, null);

            result.NumberOfDaysSinceInstallation.Should().Be(1);
        }

        [TestMethod]
        public void CreatePayload_IncludesAnalyses()
        {
            var telemetryData = new TelemetryData
            {
                Analyses = new[]
                {
                    new Analysis { Language ="cs" },
                    new Analysis { Language = "vbnet" }
                }.ToList()
            };

            var binding = CreateConfiguration(SonarLintMode.Connected, "http://localhost");

            var result = TelemetryHelper.CreatePayload(telemetryData, new DateTime(2017, 7, 25), binding, null);

            result.Analyses.Count.Should().Be(2);
            result.Analyses[0].Language.Should().Be("cs");
            result.Analyses[1].Language.Should().Be("vbnet");
        }

        [TestMethod]
        public void IsSonarCloud_InvalidUri_Null()
        {
            TelemetryHelper.IsSonarCloud(null).Should().BeFalse();
        }

        [TestMethod]
        public void IsSonarCloud_InvalidUri_Relative()
        {
            UriBuilder builder = new UriBuilder
            {
                Scheme = "file",
                Path = "..\\..\\foo\\file.txt"
            };

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

        [TestMethod]
        public void CreatePayload_VsVersionIsNull_NullVsVersionInformation()
        {
            var binding = CreateConfiguration(SonarLintMode.Connected, "https://sonarcloud.io");

            var result = TelemetryHelper.CreatePayload(
                new TelemetryData(),
                new DateTimeOffset(),
                binding,
                null);

            result.VisualStudioVersionInformation.Should().BeNull();
        }

        [TestMethod]
        public void CreatePayload_VsVersionIsNotNull_VsVersionInformation()
        {
            var vsVersion = new Mock<IVsVersion>();
            vsVersion.Setup(x => x.DisplayName).Returns("Visual Studio Enterprise 2019");
            vsVersion.Setup(x => x.InstallationVersion).Returns("16.9.30914.41");
            vsVersion.Setup(x => x.DisplayVersion).Returns("16.9.0 Preview 3.0");

            var binding = CreateConfiguration(SonarLintMode.Connected, "https://sonarcloud.io");

            // Act
            var result = TelemetryHelper.CreatePayload(
                new TelemetryData(), 
                new DateTimeOffset(),
                binding,
                vsVersion.Object);

            result.VisualStudioVersionInformation.Should().NotBeNull();
            result.VisualStudioVersionInformation.DisplayName.Should().Be("Visual Studio Enterprise 2019");
            result.VisualStudioVersionInformation.InstallationVersion.Should().Be("16.9.30914.41");
            result.VisualStudioVersionInformation.DisplayVersion.Should().Be("16.9.0 Preview 3.0");
        }

        [TestMethod]
        public void CreatePayload_StandaloneMode_ServerNotificationsAreNotSent()
        {
            var binding = BindingConfiguration.Standalone;

            var telemetryData = new TelemetryData
            {
                ServerNotifications = new ServerNotifications {IsDisabled = false}
            };

            var result = TelemetryHelper.CreatePayload(telemetryData, new DateTimeOffset(), binding, null);

            result.ServerNotifications.Should().BeNull();
        }

        [TestMethod]
        public void CreatePayload_ConnectedMode_ServerNotificationsAreSent()
        {
            var binding = CreateConfiguration(SonarLintMode.Connected, "https://sonarcloud.io");

            var telemetryData = new TelemetryData
            {
                ServerNotifications = new ServerNotifications
                {
                    IsDisabled = true,
                    ServerNotificationCounters = new ServerNotificationCounters
                    {
                        QualityGateNotificationCounter = new ServerNotificationCounter
                        {
                            ClickedCount = 11,
                            ReceivedCount = 22
                        },
                        NewIssuesNotificationCounter = new ServerNotificationCounter
                        {
                            ClickedCount = 33,
                            ReceivedCount = 44
                        }
                    }
                }
            };

            var result = TelemetryHelper.CreatePayload(telemetryData, new DateTimeOffset(), binding, null);

            result.ServerNotifications.Should().NotBeNull();
            result.ServerNotifications.IsDisabled.Should().BeTrue();
            result.ServerNotifications.ServerNotificationCounters.QualityGateNotificationCounter.ClickedCount.Should().Be(11);
            result.ServerNotifications.ServerNotificationCounters.QualityGateNotificationCounter.ReceivedCount.Should().Be(22);
            result.ServerNotifications.ServerNotificationCounters.NewIssuesNotificationCounter.ClickedCount.Should().Be(33);
            result.ServerNotifications.ServerNotificationCounters.NewIssuesNotificationCounter.ReceivedCount.Should().Be(44);
        }

        private static BindingConfiguration CreateConfiguration(SonarLintMode mode, string serverUri)
        {
            if (mode == SonarLintMode.Standalone)
            {
                if (serverUri != null)
                {
                    Assert.Fail("Test setup error: should pass a null serverUri for standalone mode");
                }
                return BindingConfiguration.Standalone;
            }

            var project = new BoundSonarQubeProject(new Uri(serverUri), "dummy.project.key", "dummy.projectName");
            return BindingConfiguration.CreateBoundConfiguration(project, mode, "c:\\test");
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
