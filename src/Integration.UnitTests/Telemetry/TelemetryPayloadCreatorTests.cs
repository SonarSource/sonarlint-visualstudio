/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Linq;
using System.Reflection;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.JsTs;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryPayloadCreatorTests
    {
        private static DateTime Now = new DateTime(2017, 7, 25, 0, 0, 0, DateTimeKind.Local).AddHours(2);

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TelemetryPayloadCreator, ITelemetryPayloadCreator>(
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<INodeVersionInfoProvider>(),
                MefTestHelpers.CreateExport<ICompatibleNodeLocator>(),
                MefTestHelpers.CreateExport<IVsVersionProvider>());
        }

        [TestMethod]
        public void Create_NullTelemetryData_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action action = () => testSubject.Create(null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("telemetryData");
        }

        [TestMethod]
        public void Create_ReturnsCorrectProductAndDates()
        {
            var telemetryData = new TelemetryData
            {
                InstallationDate = Now.AddDays(-10),
                IsAnonymousDataShared = true,
                NumberOfDaysOfUse = 5,
                ShowHotspot = new ShowHotspot { NumberOfRequests = 11 },
                TaintVulnerabilities = new TaintVulnerabilities { NumberOfIssuesInvestigatedRemotely = 44, NumberOfIssuesInvestigatedLocally = 55 },
                CFamilyProjectTypes = new CFamilyProjectTypes
                {
                    IsCMakeNonAnalyzable = true,
                    IsCMakeAnalyzable = true,
                    IsVcxNonAnalyzable = true,
                    IsVcxAnalyzable = true
                },
                RulesUsage = new RulesUsage
                {
                    DisabledByDefaultThatWereEnabled = new List<string> { "rule1", "rule2" },
                    EnabledByDefaultThatWereDisabled = new List<string> { "rule3", "rule4" },
                    RulesThatRaisedIssues = new List<string> { "rule5", "rule6" },
                    RulesWithAppliedQuickFixes = new List<string> { "rule7", "rule8" }
                }
            };

            var binding = CreateConfiguration(SonarLintMode.Connected, "https://sonarcloud.io");

            VisualStudioHelpers.VisualStudioVersion = "1.2.3.4";

            // Act
            var testSubject = CreateTestSubject(bindingConfiguration: binding);

            var result = testSubject.Create(telemetryData);

            // Assert
            result.NumberOfDaysOfUse.Should().Be(5);
            result.NumberOfDaysSinceInstallation.Should().Be(10);
            result.SonarLintProduct.Should().Be("SonarLint Visual Studio");
            result.SonarLintVersion.Should().Be(
                typeof(TelemetryData).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            result.VisualStudioVersion.Should().Be("1.2.3.4");
            result.InstallDate.Should().Be(new DateTimeOffset(Now.AddDays(-10)));
            result.SystemDate.Should().Be(new DateTimeOffset(Now));

            result.ShowHotspot.NumberOfRequests.Should().Be(11);

            result.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(44);
            result.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(55);

            result.CFamilyProjectTypes.IsCMakeNonAnalyzable.Should().BeTrue();
            result.CFamilyProjectTypes.IsCMakeAnalyzable.Should().BeTrue();
            result.CFamilyProjectTypes.IsVcxNonAnalyzable.Should().BeTrue();
            result.CFamilyProjectTypes.IsVcxAnalyzable.Should().BeTrue();

            result.RulesUsage.DisabledByDefaultThatWereEnabled.Should().BeEquivalentTo("rule1", "rule2");
            result.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo("rule3", "rule4");
            result.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule5", "rule6");
            result.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule7", "rule8");
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone, null, false, false, false)]
        [DataRow(SonarLintMode.Connected, "http://localhost", true, false, false)]
        [DataRow(SonarLintMode.Connected, "https://sonarcloud.io/", true, false, true)]
        [DataRow(SonarLintMode.LegacyConnected, "http://anotherlocalhost", true, true, false)]
        [DataRow(SonarLintMode.LegacyConnected, "https://sonarcloud.io/", true, true, true)]
        public void Create_ReturnsCorrectConnectionData(SonarLintMode mode, string serverUrl,
            bool expectedIsConnected, bool expectedIsLegacyConnected, bool expectedIsSonarCloud)
        {
            var binding = CreateConfiguration(mode, serverUrl);

            var testSubject = CreateTestSubject(bindingConfiguration: binding);

            var result = testSubject.Create(new TelemetryData());

            result.IsUsingConnectedMode.Should().Be(expectedIsConnected);
            result.IsUsingLegacyConnectedMode.Should().Be(expectedIsLegacyConnected);
            result.IsUsingSonarCloud.Should().Be(expectedIsSonarCloud);
        }

        [TestMethod]
        public void Create_NumberOfDaysSinceInstallation_On_InstallationDate()
        {
            var telemetryData = new TelemetryData
            {
                InstallationDate = Now.Subtract(new TimeSpan(23, 59, 59)) // Less than a day
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.Create(telemetryData);

            result.NumberOfDaysSinceInstallation.Should().Be(0);
        }

        [TestMethod]
        public void Create_NumberOfDaysSinceInstallation_Day_After_InstallationDate()
        {
            var telemetryData = new TelemetryData
            {
                InstallationDate = Now.AddDays(-1)
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.Create(telemetryData);

            result.NumberOfDaysSinceInstallation.Should().Be(1);
        }

        [TestMethod]
        public void Create_IncludesAnalyses()
        {
            var telemetryData = new TelemetryData
            {
                Analyses = new[]
                {
                    new Analysis { Language ="cs" },
                    new Analysis { Language = "vbnet" }
                }.ToList()
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.Create(telemetryData);

            result.Analyses.Count.Should().Be(2);
            result.Analyses[0].Language.Should().Be("cs");
            result.Analyses[1].Language.Should().Be("vbnet");
        }

        [TestMethod]
        public void IsSonarCloud_InvalidUri_Null()
        {
            TelemetryPayloadCreator.IsSonarCloud(null).Should().BeFalse();
        }

        [TestMethod]
        public void IsSonarCloud_InvalidUri_Relative()
        {
            var builder = new UriBuilder
            {
                Scheme = "file",
                Path = "..\\..\\foo\\file.txt"
            };

            TelemetryPayloadCreator.IsSonarCloud(builder.Uri).Should().BeFalse();
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
        public void Create_VsVersionIsNull_NullVsVersionInformation()
        {
            var testSubject = CreateTestSubject(visualStudioVersion:null);

            var result = testSubject.Create(new TelemetryData());

            result.VisualStudioVersionInformation.Should().BeNull();
        }

        [TestMethod]
        public void Create_VsVersionIsNotNull_VsVersionInformation()
        {
            var vsVersion = new Mock<IVsVersion>();
            vsVersion.Setup(x => x.DisplayName).Returns("Visual Studio Enterprise 2019");
            vsVersion.Setup(x => x.InstallationVersion).Returns("16.9.30914.41");
            vsVersion.Setup(x => x.DisplayVersion).Returns("16.9.0 Preview 3.0");

            var testSubject = CreateTestSubject(visualStudioVersion: vsVersion.Object);

            var result = testSubject.Create(new TelemetryData());

            result.VisualStudioVersionInformation.Should().NotBeNull();
            result.VisualStudioVersionInformation.DisplayName.Should().Be("Visual Studio Enterprise 2019");
            result.VisualStudioVersionInformation.InstallationVersion.Should().Be("16.9.30914.41");
            result.VisualStudioVersionInformation.DisplayVersion.Should().Be("16.9.0 Preview 3.0");
        }

        [TestMethod]
        public void Create_StandaloneMode_ServerNotificationsAreNotSent()
        {
            var binding = BindingConfiguration.Standalone;

            var telemetryData = new TelemetryData
            {
                ServerNotifications = new ServerNotifications { IsDisabled = false }
            };

            var testSubject = CreateTestSubject(bindingConfiguration: binding);
            var result = testSubject.Create(telemetryData);

            result.ServerNotifications.Should().BeNull();
        }

        [TestMethod]
        public void Create_ConnectedMode_ServerNotificationsAreSent()
        {
            var binding = CreateConfiguration(SonarLintMode.Connected, "https://sonarcloud.io");

            var telemetryData = new TelemetryData
            {
                ServerNotifications = new ServerNotifications
                {
                    IsDisabled = true,
                    ServerNotificationCounters = new Dictionary<string, ServerNotificationCounter>
                    {
                        {"QUALITY_GATE", new ServerNotificationCounter
                        {
                            ReceivedCount = 22,
                            ClickedCount = 11
                        }},
                        {"NEW_ISSUES", new ServerNotificationCounter
                        {
                            ReceivedCount = 44,
                            ClickedCount = 33
                        }}
                    }
                }
            };

            var testSubject = CreateTestSubject(bindingConfiguration: binding);
            var result = testSubject.Create(telemetryData);

            result.ServerNotifications.Should().NotBeNull();
            result.ServerNotifications.IsDisabled.Should().BeTrue();
            result.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ClickedCount.Should().Be(11);
            result.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ReceivedCount.Should().Be(22);
            result.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ClickedCount.Should().Be(33);
            result.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ReceivedCount.Should().Be(44);
        }

        [TestMethod]
        public void Create_NoCompatibleNodeVersion_NullCompatibleVersion()
        {
            var testSubject = CreateTestSubject(compatibleNodeVersion: null);

            var result = testSubject.Create(new TelemetryData());

            result.CompatibleNodeJsVersion.Should().BeNull();
        }

        [TestMethod]
        public void Create_HasCompatibleNodeVersion_CompatibleVersion()
        {
            var version = new NodeVersionInfo("some exe", new Version(1, 2, 3, 4));
            var testSubject = CreateTestSubject(compatibleNodeVersion: version);

            var result = testSubject.Create(new TelemetryData());

            result.CompatibleNodeJsVersion.Should().Be("1.2.3.4");
        }

        [TestMethod]
        public void Create_NoDetectedNodeVersion_NullMaxVersion()
        {
            var testSubject = CreateTestSubject(allNodeVersions: null);

            var result = testSubject.Create(new TelemetryData());

            result.MaxNodeJsVersion.Should().BeNull();
        }

        [TestMethod]
        public void Create_HasOneNodeVersion_VersionSetAsMaxVersion()
        {
            var version = new NodeVersionInfo("some exe", new Version(1, 2, 3, 4));
            var testSubject = CreateTestSubject(allNodeVersions: new[] { version });

            var result = testSubject.Create(new TelemetryData());

            result.MaxNodeJsVersion.Should().Be("1.2.3.4");
        }

        [TestMethod]
        public void Create_HasMultipleNodeVersions_TakesMaxVersion()
        {
            var version1 = new NodeVersionInfo("some exe2", new Version(0, 9, 9, 9)); // 4th
            var version2 = new NodeVersionInfo("some exe2", new Version(1, 2, 3, 4)); // 3rd
            var version3 = new NodeVersionInfo("some exe1", new Version(2, 0, 0, 1)); // 1st
            var version4 = new NodeVersionInfo("some exe2", new Version(1, 2, 3, 6)); // 2nd

            var testSubject = CreateTestSubject(allNodeVersions: new[] { version1, version2, version3, version4 });

            var result = testSubject.Create(new TelemetryData());

            result.MaxNodeJsVersion.Should().Be("2.0.0.1");
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
            TelemetryPayloadCreator.IsSonarCloud(new Uri(uri)).Should().BeFalse();
        }

        private static void CheckIsSonarCloud(string uri)
        {
            TelemetryPayloadCreator.IsSonarCloud(new Uri(uri)).Should().BeTrue();
        }

        private TelemetryPayloadCreator CreateTestSubject(BindingConfiguration bindingConfiguration = null,
            IVsVersion visualStudioVersion = null,
            NodeVersionInfo compatibleNodeVersion = null,
            NodeVersionInfo[] allNodeVersions = null)
        {
            bindingConfiguration ??= CreateConfiguration(SonarLintMode.LegacyConnected, "http://localhost");

            var solutionBindingTracker = new Mock<IActiveSolutionBoundTracker>();
            solutionBindingTracker.Setup(x => x.CurrentConfiguration).Returns(bindingConfiguration);

            var vsVersionProvider = new Mock<IVsVersionProvider>();
            vsVersionProvider.Setup(x => x.Version).Returns(visualStudioVersion);

            var currentTimeProvider = new Mock<ICurrentTimeProvider>();
            currentTimeProvider.Setup(x => x.Now).Returns(new DateTimeOffset(Now));

            var compatibleNodeLocator = new Mock<ICompatibleNodeLocator>();
            compatibleNodeLocator.Setup(x => x.Locate()).Returns(compatibleNodeVersion);

            allNodeVersions ??= Array.Empty<NodeVersionInfo>();

            var nodeVersionInfoProvider = new Mock<INodeVersionInfoProvider>();
            nodeVersionInfoProvider.Setup(x => x.GetAllNodeVersions()).Returns(allNodeVersions);

            return new TelemetryPayloadCreator(solutionBindingTracker.Object,
                vsVersionProvider.Object,
                nodeVersionInfoProvider.Object,
                compatibleNodeLocator.Object,
                currentTimeProvider.Object);
        }
    }
}
