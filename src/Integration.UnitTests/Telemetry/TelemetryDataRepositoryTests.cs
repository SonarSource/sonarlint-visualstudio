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
using System.IO;
using System.IO.Abstractions;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryDataRepositoryTests
    {
        private Mock<IFileSystem> fileSystemMock;
        private Mock<IFileSystemWatcherFactory> watcherFactoryMock;

        [TestMethod]
        public void Ctor_Create_Storage_File()
        {
            var fileContents = new StringBuilder();
            // Arrange
            InitializeMocks(fileContents, fileExists: false, dirExists: false);

            fileSystemMock
                .Setup(x => x.Directory.CreateDirectory(Path.GetDirectoryName(TelemetryDataRepository.GetStorageFilePath())))
                .Returns(null as IDirectoryInfo);

            fileSystemMock
                .Setup(x => x.File.WriteAllText(TelemetryDataRepository.GetStorageFilePath(), It.IsAny<string>()))
                .Callback((string path, string content) => fileContents.Append(content));

            // Act
            var repository = new TelemetryDataRepository(fileSystemMock.Object, watcherFactoryMock.Object);

            // Assert
            RemoveLineEndings(fileContents.ToString()).Should().Be(RemoveLineEndings(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IsAnonymousDataShared>true</IsAnonymousDataShared>
  <NumberOfDaysOfUse>0</NumberOfDaysOfUse>
  <NumberOfShowHotspotRequests>0</NumberOfShowHotspotRequests>
  <InstallationDate>0001-01-01T00:00:00.0000000+00:00</InstallationDate>
  <LastSavedAnalysisDate>0001-01-01T00:00:00.0000000+00:00</LastSavedAnalysisDate>
  <LastUploadDate>0001-01-01T00:00:00.0000000+00:00</LastUploadDate>
  <Analyses />
</TelemetryData>"));

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock);
        }

        [TestMethod]
        public void Ctor_Reads_Value_From_File()
        {
            // Arrange
            var fileContents = new StringBuilder(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IsAnonymousDataShared>false</IsAnonymousDataShared>
  <NumberOfDaysOfUse>10</NumberOfDaysOfUse>
  <NumberOfShowHotspotRequests>20</NumberOfShowHotspotRequests>
  <InstallationDate>2017-03-15T06:15:42.1234567+01:00</InstallationDate>
  <LastSavedAnalysisDate>2018-03-15T06:15:42.1234567+01:00</LastSavedAnalysisDate>
  <LastUploadDate>2019-03-15T06:15:42.1234567+01:00</LastUploadDate>
</TelemetryData>");

            InitializeMocks(fileContents, fileExists: true, dirExists: true);

            // Act
            var repository = new TelemetryDataRepository(fileSystemMock.Object, watcherFactoryMock.Object);

            // Assert
            repository.Data.IsAnonymousDataShared.Should().BeFalse();
            repository.Data.NumberOfDaysOfUse.Should().Be(10);
            repository.Data.NumberOfShowHotspotRequests.Should().Be(20);
            repository.Data.InstallationDate.Should().Be(new DateTimeOffset(new DateTime(2017, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));
            repository.Data.LastSavedAnalysisDate.Should().Be(new DateTimeOffset(new DateTime(2018, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));
            repository.Data.LastUploadDate.Should().Be(new DateTimeOffset(new DateTime(2019, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock);
        }

        [TestMethod]
        public void Instance_Reads_File_On_Change()
        {
            // Arrange
            var fileContents = new StringBuilder(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IsAnonymousDataShared>false</IsAnonymousDataShared>
  <NumberOfDaysOfUse>10</NumberOfDaysOfUse>
  <InstallationDate>2010-03-15T06:15:42.1234567+01:00</InstallationDate>
  <LastSavedAnalysisDate>2010-03-15T06:15:42.1234567+01:00</LastSavedAnalysisDate>
  <LastUploadDate>2010-03-15T06:15:42.1234567+01:00</LastUploadDate>
</TelemetryData>");

            var fileSystemWatcherMock = new Mock<IFileSystemWatcher>();
            InitializeMocks(fileContents, fileExists: true, dirExists: true, fileSystemWatcher: fileSystemWatcherMock.Object);

            var repository = new TelemetryDataRepository(fileSystemMock.Object, watcherFactoryMock.Object);

            // Act
            const bool newIsAnonymousDataShared = true;
            const int newDaysOfUse = 15;
            const int newHotspotsRequests = 25;
            var newInstallationDate = new DateTimeOffset(new DateTime(2017, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            var newLastSavedAnalysisDate = new DateTimeOffset(new DateTime(2018, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            var newLastUploadDate = new DateTimeOffset(new DateTime(2019, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            fileContents.Clear();
            fileContents.Append($@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IsAnonymousDataShared>{newIsAnonymousDataShared.ToString().ToLower()}</IsAnonymousDataShared>
  <NumberOfDaysOfUse>{newDaysOfUse}</NumberOfDaysOfUse>
  <NumberOfShowHotspotRequests>{newHotspotsRequests}</NumberOfShowHotspotRequests>
  <InstallationDate>{newInstallationDate.ToString("o")}</InstallationDate>
  <LastSavedAnalysisDate>{newLastSavedAnalysisDate.ToString("o")}</LastSavedAnalysisDate>
  <LastUploadDate>{newLastUploadDate.ToString("o")}</LastUploadDate>
</TelemetryData>");

            fileSystemWatcherMock
                .Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            // Assert
            repository.Data.IsAnonymousDataShared.Should().Be(newIsAnonymousDataShared);
            repository.Data.NumberOfDaysOfUse.Should().Be(newDaysOfUse);
            repository.Data.NumberOfShowHotspotRequests.Should().Be(newHotspotsRequests);
            repository.Data.InstallationDate.Should().Be(newInstallationDate);
            repository.Data.LastSavedAnalysisDate.Should().Be(newLastSavedAnalysisDate);
            repository.Data.LastUploadDate.Should().Be(newLastUploadDate);

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock, fileSystemWatcherMock);
        }

        [TestMethod]
        public void Can_Read_Old_TelemetryXml()
        {
            var fileContents = new StringBuilder(@"<?xml version=""1.0"" encoding=""utf-8""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
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
            var repository = new TelemetryDataRepository(fileSystemMock.Object, watcherFactoryMock.Object);

            // Assert
            repository.Data.InstallationDate.Should().Be(expectedDateTimeOffset);
            repository.Data.LastSavedAnalysisDate.Should().Be(expectedDateTimeOffset);
            repository.Data.NumberOfDaysOfUse.Should().Be(5807);
            repository.Data.NumberOfShowHotspotRequests.Should().Be(0);
            repository.Data.LastUploadDate.Should().Be(expectedDateTimeOffset);
            repository.Data.IsAnonymousDataShared.Should().BeFalse();

            Mock.VerifyAll(fileSystemMock, watcherFactoryMock);
        }

        private void InitializeMocks(StringBuilder fileContents, bool fileExists, bool dirExists,
            IFileSystemWatcher fileSystemWatcher = null)
        {
            fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);

            fileSystemMock
                .Setup(x => x.File.ReadAllText(TelemetryDataRepository.GetStorageFilePath()))
                .Returns(fileContents.ToString);

            fileSystemMock
                .Setup(x => x.File.Exists(TelemetryDataRepository.GetStorageFilePath()))
                .Returns(fileExists);

            fileSystemMock
                .Setup(x => x.Directory.Exists(Path.GetDirectoryName(TelemetryDataRepository.GetStorageFilePath())))
                .Returns(dirExists);

            watcherFactoryMock = new Mock<IFileSystemWatcherFactory>(MockBehavior.Strict);
            watcherFactoryMock
                .Setup(x => x.CreateNew())
                .Returns(fileSystemWatcher ?? new Mock<IFileSystemWatcher>().Object);
        }

        private string RemoveLineEndings(string text)
        {
            return text.Replace("\r\n", string.Empty).Replace("\n", string.Empty);
        }
    }
}
