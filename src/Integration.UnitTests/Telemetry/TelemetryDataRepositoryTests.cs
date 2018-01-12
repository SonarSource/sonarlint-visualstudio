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
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryDataRepositoryTests
    {
        private Mock<IFile> fileMock;
        private Mock<IFileSystemWatcherFactory> watcherFactoryMock;
        private static Mock<IDirectory> directoryMock;

        [TestMethod]
        public void Ctor_Create_Storage_File()
        {
            // Arrange
            var fileContents = new StringBuilder();

            InitializeMocks(fileContents, fileExists: false, dirExists: false);
            directoryMock
                .Setup(x => x.Create(Path.GetDirectoryName(TelemetryDataRepository.GetStorageFilePath())));
            fileMock
               .Setup(x => x.CreateText(TelemetryDataRepository.GetStorageFilePath()))
               .Returns(() => new StringWriter(fileContents));

            // Act
            var repository = new TelemetryDataRepository(fileMock.Object, directoryMock.Object, watcherFactoryMock.Object);

            // Assert
            RemoveLineEndings(fileContents.ToString()).Should().Be(RemoveLineEndings(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IsAnonymousDataShared>true</IsAnonymousDataShared>
  <NumberOfDaysOfUse>0</NumberOfDaysOfUse>
  <InstallationDate>0001-01-01T00:00:00.0000000+00:00</InstallationDate>
  <LastSavedAnalysisDate>0001-01-01T00:00:00.0000000+00:00</LastSavedAnalysisDate>
  <LastUploadDate>0001-01-01T00:00:00.0000000+00:00</LastUploadDate>
</TelemetryData>"));

            Mock.VerifyAll(fileMock, directoryMock, watcherFactoryMock);
        }

        [TestMethod]
        public void Ctor_Reads_Value_From_File()
        {
            // Arrange
            var fileContents = new StringBuilder(@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IsAnonymousDataShared>false</IsAnonymousDataShared>
  <NumberOfDaysOfUse>10</NumberOfDaysOfUse>
  <InstallationDate>2017-03-15T06:15:42.1234567+01:00</InstallationDate>
  <LastSavedAnalysisDate>2018-03-15T06:15:42.1234567+01:00</LastSavedAnalysisDate>
  <LastUploadDate>2019-03-15T06:15:42.1234567+01:00</LastUploadDate>
</TelemetryData>");

            InitializeMocks(fileContents, fileExists: true, dirExists: true);

            // Act
            var repository = new TelemetryDataRepository(fileMock.Object, directoryMock.Object, watcherFactoryMock.Object);

            // Assert
            repository.Data.IsAnonymousDataShared.Should().BeFalse();
            repository.Data.NumberOfDaysOfUse.Should().Be(10);
            repository.Data.InstallationDate.Should().Be(new DateTimeOffset(new DateTime(2017, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));
            repository.Data.LastSavedAnalysisDate.Should().Be(new DateTimeOffset(new DateTime(2018, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));
            repository.Data.LastUploadDate.Should().Be(new DateTimeOffset(new DateTime(2019, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1)));

            Mock.VerifyAll(fileMock, directoryMock, watcherFactoryMock);
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

            var repository = new TelemetryDataRepository(fileMock.Object, directoryMock.Object, watcherFactoryMock.Object);

            // Act
            var newIsAnonymousDataShared = true;
            var newDaysOfUse = 15;
            var newInstallationDate = new DateTimeOffset(new DateTime(2017, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            var newLastSavedAnalysisDate = new DateTimeOffset(new DateTime(2018, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            var newLastUploadDate = new DateTimeOffset(new DateTime(2019, 3, 15, 6, 15, 42, 123).AddTicks(4567), TimeSpan.FromHours(1));
            fileContents.Clear();
            fileContents.Append($@"<?xml version=""1.0"" encoding=""utf-16""?>
<TelemetryData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IsAnonymousDataShared>{newIsAnonymousDataShared.ToString().ToLower()}</IsAnonymousDataShared>
  <NumberOfDaysOfUse>{newDaysOfUse}</NumberOfDaysOfUse>
  <InstallationDate>{newInstallationDate.ToString("o")}</InstallationDate>
  <LastSavedAnalysisDate>{newLastSavedAnalysisDate.ToString("o")}</LastSavedAnalysisDate>
  <LastUploadDate>{newLastUploadDate.ToString("o")}</LastUploadDate>
</TelemetryData>");

            fileSystemWatcherMock
                .Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            // Assert
            repository.Data.IsAnonymousDataShared.Should().Be(newIsAnonymousDataShared);
            repository.Data.NumberOfDaysOfUse.Should().Be(newDaysOfUse);
            repository.Data.InstallationDate.Should().Be(newInstallationDate);
            repository.Data.LastSavedAnalysisDate.Should().Be(newLastSavedAnalysisDate);
            repository.Data.LastUploadDate.Should().Be(newLastUploadDate);

            Mock.VerifyAll(fileMock, directoryMock, watcherFactoryMock, fileSystemWatcherMock);
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

            InitializeMocks(fileContents, fileExists: true, dirExists: true);

            // Act
            var repository = new TelemetryDataRepository(fileMock.Object, directoryMock.Object, watcherFactoryMock.Object);

            // Assert
            repository.Data.InstallationDate.Should().Be(new DateTimeOffset(1999, 12, 31, 23, 59, 59, 999, DateTimeOffset.Now.Offset).AddTicks(9999));
            repository.Data.LastSavedAnalysisDate.Should().Be(new DateTimeOffset(1999, 12, 31, 23, 59, 59, 999, DateTimeOffset.Now.Offset).AddTicks(9999));
            repository.Data.NumberOfDaysOfUse.Should().Be(5807);
            repository.Data.LastUploadDate.Should().Be(new DateTimeOffset(1999, 12, 31, 23, 59, 59, 999, DateTimeOffset.Now.Offset).AddTicks(9999));
            repository.Data.IsAnonymousDataShared.Should().BeFalse();

            Mock.VerifyAll(fileMock, directoryMock, watcherFactoryMock);
        }

        private void InitializeMocks(StringBuilder fileContents, bool fileExists, bool dirExists,
            IFileSystemWatcher fileSystemWatcher = null)
        {
            fileMock = new Mock<IFile>(MockBehavior.Strict);
            fileMock
                .Setup(x => x.OpenText(TelemetryDataRepository.GetStorageFilePath()))
                .Returns(() => new StringReader(fileContents.ToString()));
            fileMock
                .Setup(x => x.Exists(TelemetryDataRepository.GetStorageFilePath()))
                .Returns(fileExists);

            directoryMock = new Mock<IDirectory>(MockBehavior.Strict);
            directoryMock
                .Setup(x => x.Exists(Path.GetDirectoryName(TelemetryDataRepository.GetStorageFilePath())))
                .Returns(dirExists);

            watcherFactoryMock = new Mock<IFileSystemWatcherFactory>(MockBehavior.Strict);
            watcherFactoryMock
                .Setup(x => x.Create())
                .Returns(fileSystemWatcher ?? new Mock<IFileSystemWatcher>().Object);
        }

        private string RemoveLineEndings(string text)
        {
            return text.Replace("\r\n", string.Empty).Replace("\n", string.Empty);
        }
    }
}
