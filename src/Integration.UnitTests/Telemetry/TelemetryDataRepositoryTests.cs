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

using System.IO;
using System.IO.Abstractions;
using System.Text;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryDataRepositoryTests
    {
        private Mock<IFileSystem> fileSystemMock;
        private Mock<IFileSystemWatcherFactory> watcherFactoryMock;
        private Mock<IEnvironmentVariableProvider> environmentVariableProviderMock;

        [TestMethod]
        public void Ctor_Create_Storage_File()
        {
            var fileContents = new StringBuilder();
            // Arrange
            InitializeMocks(fileContents, fileExists: false, dirExists: false);

            fileSystemMock
                .Setup(x => x.Directory.CreateDirectory(Path.GetDirectoryName(TelemetryDataRepository.GetStorageFilePath(environmentVariableProviderMock.Object))))
                .Returns(null as IDirectoryInfo);

            fileSystemMock
                .Setup(x => x.File.WriteAllText(TelemetryDataRepository.GetStorageFilePath(environmentVariableProviderMock.Object), It.IsAny<string>()))
                .Callback((string path, string content) => fileContents.Append(content));

            // Act
            var repository = CreateTestSubject();

            // Assert
            fileContents.ToString().Should().Be(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <IsAnonymousDataShared>true</IsAnonymousDataShared>
  <NumberOfDaysOfUse>0</NumberOfDaysOfUse>
  <InstallationDate>0001-01-01T00:00:00.0000000+00:00</InstallationDate>
  <LastSavedAnalysisDate>0001-01-01T00:00:00.0000000+00:00</LastSavedAnalysisDate>
  <LastUploadDate>0001-01-01T00:00:00.0000000+00:00</LastUploadDate>
  <Analyses />
  <ShowHotspot>
    <NumberOfRequests>0</NumberOfRequests>
  </ShowHotspot>
  <TaintVulnerabilities>
    <NumberOfIssuesInvestigatedLocally>0</NumberOfIssuesInvestigatedLocally>
    <NumberOfIssuesInvestigatedRemotely>0</NumberOfIssuesInvestigatedRemotely>
  </TaintVulnerabilities>
  <ServerNotifications>
    <IsDisabled>false</IsDisabled>
    <ServerNotificationCounters />
  </ServerNotifications>
  <CFamilyProjectTypes>
    <IsCMakeAnalyzable>false</IsCMakeAnalyzable>
    <IsCMakeNonAnalyzable>false</IsCMakeNonAnalyzable>
    <IsVcxAnalyzable>false</IsVcxAnalyzable>
    <IsVcxNonAnalyzable>false</IsVcxNonAnalyzable>
  </CFamilyProjectTypes>
  <RulesUsage>
    <EnabledByDefaultThatWereDisabled />
    <DisabledByDefaultThatWereEnabled />
    <RulesThatRaisedIssues />
    <RulesWithAppliedQuickFixes />
  </RulesUsage>
</TelemetryData>");

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock);
        }

        [TestMethod]
        public void Ctor_Reads_Value_From_File()
        {
            // Arrange
            var fileContents = new StringBuilder(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData  xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <IsAnonymousDataShared>false</IsAnonymousDataShared>
  <NumberOfDaysOfUse>10</NumberOfDaysOfUse>
  <InstallationDate>2017-03-15T06:15:42.1234567+01:00</InstallationDate>
  <LastSavedAnalysisDate>2018-03-15T06:15:42.1234567+01:00</LastSavedAnalysisDate>
  <LastUploadDate>2019-03-15T06:15:42.1234567+01:00</LastUploadDate>
  <ShowHotspot>
    <NumberOfRequests>20</NumberOfRequests>
  </ShowHotspot>
  <TaintVulnerabilities>
    <NumberOfIssuesInvestigatedLocally>66</NumberOfIssuesInvestigatedLocally>
    <NumberOfIssuesInvestigatedRemotely>55</NumberOfIssuesInvestigatedRemotely>
  </TaintVulnerabilities>
  <ServerNotifications>
    <IsDisabled>true</IsDisabled>
    <ServerNotificationCounters>
      <KeyValue>
        <Key>QUALITY_GATE</Key>
        <Value>
          <ReceivedCount>11</ReceivedCount>
          <ClickedCount>22</ClickedCount>
        </Value>
      </KeyValue>
      <KeyValue>
        <Key>NEW_ISSUES</Key>
        <Value>
          <ReceivedCount>33</ReceivedCount>
          <ClickedCount>44</ClickedCount>
        </Value>
      </KeyValue>
    </ServerNotificationCounters>
  </ServerNotifications>
  <RulesUsage>
    <EnabledByDefaultThatWereDisabled>
      <string>rule1</string> 
      <string>rule2</string> 
    </EnabledByDefaultThatWereDisabled>
    <DisabledByDefaultThatWereEnabled>
      <string>rule3</string> 
      <string>rule4</string> 
    </DisabledByDefaultThatWereEnabled>
    <RulesThatRaisedIssues>
      <string>rule5</string> 
      <string>rule6</string> 
    </RulesThatRaisedIssues>
    <RulesWithAppliedQuickFixes>
      <string>rule7</string> 
      <string>rule8</string> 
    </RulesWithAppliedQuickFixes>
  </RulesUsage>
</TelemetryData>");

            InitializeMocks(fileContents, fileExists: true, dirExists: true);

            // Act
            var repository = CreateTestSubject();

            // Assert
            repository.Data.IsAnonymousDataShared.Should().BeFalse();
            repository.Data.NumberOfDaysOfUse.Should().Be(10);
            repository.Data.InstallationDate.Should().Be(new DateTimeOffset(new DateTime(2017, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));
            repository.Data.LastSavedAnalysisDate.Should().Be(new DateTimeOffset(new DateTime(2018, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));
            repository.Data.LastUploadDate.Should().Be(new DateTimeOffset(new DateTime(2019, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));

            repository.Data.ShowHotspot.NumberOfRequests.Should().Be(20);

            repository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(55);
            repository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(66);

            repository.Data.ServerNotifications.IsDisabled.Should().BeTrue();
            repository.Data.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ClickedCount.Should().Be(22);
            repository.Data.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ReceivedCount.Should().Be(11);
            repository.Data.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ClickedCount.Should().Be(44);
            repository.Data.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ReceivedCount.Should().Be(33);

            repository.Data.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo("rule1", "rule2");
            repository.Data.RulesUsage.DisabledByDefaultThatWereEnabled.Should().BeEquivalentTo("rule3", "rule4");
            repository.Data.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule5", "rule6");
            repository.Data.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule7", "rule8");

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock);
        }

        [TestMethod]
        public void Instance_Reads_File_On_Change()
        {
            // Arrange
            var fileContents = new StringBuilder(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData  xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <IsAnonymousDataShared>false</IsAnonymousDataShared>
  <NumberOfDaysOfUse>10</NumberOfDaysOfUse>
  <InstallationDate>2010-03-15T06:15:42.1234567+01:00</InstallationDate>
  <LastSavedAnalysisDate>2010-03-15T06:15:42.1234567+01:00</LastSavedAnalysisDate>
  <LastUploadDate>2010-03-15T06:15:42.1234567+01:00</LastUploadDate>
</TelemetryData>");

            var fileSystemWatcherMock = new Mock<IFileSystemWatcher>();
            InitializeMocks(fileContents, fileExists: true, dirExists: true, fileSystemWatcher: fileSystemWatcherMock.Object);

            var repository = CreateTestSubject();

            // Act
            const bool newIsAnonymousDataShared = true;
            const int newDaysOfUse = 15;
            const int newHotspotsRequests = 25;
            const int newTaintRedirects = 7;
            const int newTaintOpenedIssues = 9;
            const bool notificationsDisabled = true;
            const int qualityGateReceivedCount = 1234;
            const int qualityGateClickedCount = 5678;
            const int newIssuesReceivedCount = 8765;
            const int newIssuesClickedCount = 4321;

            var newInstallationDate = new DateTimeOffset(new DateTime(2017, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            var newLastSavedAnalysisDate = new DateTimeOffset(new DateTime(2018, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            var newLastUploadDate = new DateTimeOffset(new DateTime(2019, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            fileContents.Clear();
            fileContents.Append($@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData  xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <IsAnonymousDataShared>{newIsAnonymousDataShared.ToString().ToLower()}</IsAnonymousDataShared>
  <NumberOfDaysOfUse>{newDaysOfUse}</NumberOfDaysOfUse>
  <InstallationDate>{newInstallationDate.ToString("o")}</InstallationDate>
  <LastSavedAnalysisDate>{newLastSavedAnalysisDate.ToString("o")}</LastSavedAnalysisDate>
  <LastUploadDate>{newLastUploadDate.ToString("o")}</LastUploadDate>
  <ShowHotspot>
    <NumberOfRequests>{newHotspotsRequests}</NumberOfRequests>
  </ShowHotspot>
  <TaintVulnerabilities>
    <NumberOfIssuesInvestigatedLocally>{newTaintOpenedIssues}</NumberOfIssuesInvestigatedLocally>
    <NumberOfIssuesInvestigatedRemotely>{newTaintRedirects}</NumberOfIssuesInvestigatedRemotely>
  </TaintVulnerabilities>
  <ServerNotifications>
    <IsDisabled>{notificationsDisabled.ToString().ToLower()}</IsDisabled>
    <ServerNotificationCounters>
      <KeyValue>
        <Key>QUALITY_GATE</Key>
        <Value>
          <ReceivedCount>{qualityGateReceivedCount}</ReceivedCount>
          <ClickedCount>{qualityGateClickedCount}</ClickedCount>
        </Value>
      </KeyValue>
      <KeyValue>
        <Key>NEW_ISSUES</Key>
        <Value>
          <ReceivedCount>{newIssuesReceivedCount}</ReceivedCount>
          <ClickedCount>{newIssuesClickedCount}</ClickedCount>
        </Value>
      </KeyValue>
    </ServerNotificationCounters>
  </ServerNotifications>
  <RulesUsage>
    <EnabledByDefaultThatWereDisabled>
      <string>rule11</string> 
      <string>rule12</string> 
    </EnabledByDefaultThatWereDisabled>
    <DisabledByDefaultThatWereEnabled>
      <string>rule13</string> 
      <string>rule14</string> 
    </DisabledByDefaultThatWereEnabled>
    <RulesThatRaisedIssues>
      <string>rule15</string> 
      <string>rule16</string> 
    </RulesThatRaisedIssues>
    <RulesWithAppliedQuickFixes>
      <string>rule17</string> 
      <string>rule18</string> 
    </RulesWithAppliedQuickFixes>
  </RulesUsage>
</TelemetryData>");

            fileSystemWatcherMock
                .Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            // Assert
            repository.Data.IsAnonymousDataShared.Should().Be(newIsAnonymousDataShared);
            repository.Data.NumberOfDaysOfUse.Should().Be(newDaysOfUse);
            repository.Data.InstallationDate.Should().Be(newInstallationDate);
            repository.Data.LastSavedAnalysisDate.Should().Be(newLastSavedAnalysisDate);
            repository.Data.LastUploadDate.Should().Be(newLastUploadDate);

            repository.Data.ShowHotspot.NumberOfRequests.Should().Be(newHotspotsRequests);

            repository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(newTaintRedirects);
            repository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(newTaintOpenedIssues);

            repository.Data.ServerNotifications.IsDisabled.Should().Be(notificationsDisabled);
            repository.Data.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ClickedCount.Should().Be(qualityGateClickedCount);
            repository.Data.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ReceivedCount.Should().Be(qualityGateReceivedCount);
            repository.Data.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ClickedCount.Should().Be(newIssuesClickedCount);
            repository.Data.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ReceivedCount.Should().Be(newIssuesReceivedCount);

            repository.Data.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo("rule11", "rule12");
            repository.Data.RulesUsage.DisabledByDefaultThatWereEnabled.Should().BeEquivalentTo("rule13", "rule14");
            repository.Data.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule15", "rule16");
            repository.Data.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule17", "rule18");

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock, fileSystemWatcherMock);
        }

        [TestMethod]
        public void Can_Read_Old_TelemetryXml()
        {
            var fileContents = new StringBuilder(@"<?xml version=""1.0"" encoding=""utf-8""?>
<TelemetryData  xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
 <IsAnonymousDataShared>false</IsAnonymousDataShared>
 <InstallationDate>1999-12-31T23:59:59.9999999</InstallationDate>
 <LastSavedAnalysisDate>1999-12-31T23:59:59.9999999</LastSavedAnalysisDate>
 <NumberOfDaysOfUse>5807</NumberOfDaysOfUse>
 <LastUploadDate>1999-12-31T23:59:59.9999999</LastUploadDate>
</TelemetryData>");

            // Calculate the expected result.
            // Previously, this test started failing when daylight saving was applied on the test agent
            // machine. Creating the local date first then converting it to a DateTimeOffset gives the
            // expected result regardless of the local time zone or whether the test agent machine is
            // automatically adjusting dor daylight saving time or not.
            var expectedDate = new DateTime(1999, 12, 31, 23, 59, 59, 999, DateTimeKind.Local).AddTicks(9999);
            var expectedDateTimeOffset = new DateTimeOffset(expectedDate);

            InitializeMocks(fileContents, fileExists: true, dirExists: true);

            // Act
            var repository = CreateTestSubject();

            // Assert
            repository.Data.InstallationDate.Should().Be(expectedDateTimeOffset);
            repository.Data.LastSavedAnalysisDate.Should().Be(expectedDateTimeOffset);
            repository.Data.NumberOfDaysOfUse.Should().Be(5807);
            repository.Data.LastUploadDate.Should().Be(expectedDateTimeOffset);
            repository.Data.IsAnonymousDataShared.Should().BeFalse();
            repository.Data.ShowHotspot.NumberOfRequests.Should().Be(0);
            repository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(0);
            repository.Data.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(0);

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock);
        }

        private void InitializeMocks(StringBuilder fileContents, bool fileExists, bool dirExists,
            IFileSystemWatcher fileSystemWatcher = null, string rootFolderPath = "c:\\users\\foo")
        {
            environmentVariableProviderMock = new Mock<IEnvironmentVariableProvider>();
            environmentVariableProviderMock.Setup(x => x.GetFolderPath(Environment.SpecialFolder.ApplicationData))
                .Returns(rootFolderPath);

            var expectedFilePath = TelemetryDataRepository.GetStorageFilePath(environmentVariableProviderMock.Object);

            fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);

            fileSystemMock
                .Setup(x => x.File.ReadAllText(expectedFilePath))
                .Returns(fileContents.ToString);

            fileSystemMock
                .Setup(x => x.File.Exists(expectedFilePath))
                .Returns(fileExists);

            fileSystemMock
                .Setup(x => x.Directory.Exists(Path.GetDirectoryName(expectedFilePath)))
                .Returns(dirExists);

            watcherFactoryMock = new Mock<IFileSystemWatcherFactory>(MockBehavior.Strict);
            watcherFactoryMock
                .Setup(x => x.CreateNew())
                .Returns(fileSystemWatcher ?? new Mock<IFileSystemWatcher>().Object);
        }

        private TelemetryDataRepository CreateTestSubject()
            => new TelemetryDataRepository(fileSystemMock.Object, watcherFactoryMock.Object, environmentVariableProviderMock.Object);
    }
}
