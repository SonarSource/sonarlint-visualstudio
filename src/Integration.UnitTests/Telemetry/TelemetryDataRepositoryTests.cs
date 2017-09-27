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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryDataRepositoryTests
    {
        [TestMethod]
        public void Ctor_AlwaysCreateStorageFileFolders()
        {
            // Arrange
            var filePath = TelemetryDataRepository.GetStorageFilePath();
            var directoryPath = Path.GetDirectoryName(filePath);

            RetryHelper.RetryOnException(5, TimeSpan.FromSeconds(1), () => Directory.Delete(directoryPath, true));
            Thread.Sleep(500);
            Directory.Exists(directoryPath).Should().BeFalse(); // Sanity test

            // Act
            var repository = new TelemetryDataRepository();

            // Assert
            Directory.Exists(directoryPath).Should().BeTrue();
        }

        [TestMethod]
        public void Ctor_AlwaysCreateStorageFile()
        {
            // Arrange
            var filePath = TelemetryDataRepository.GetStorageFilePath();

            RetryHelper.RetryOnException(5, TimeSpan.FromSeconds(1), () => File.Delete(filePath));
            Thread.Sleep(500);
            File.Exists(filePath).Should().BeFalse(); // Sanity test

            // Act
            var repository = new TelemetryDataRepository();

            // Assert
            File.Exists(filePath).Should().BeTrue();
        }

        [TestMethod]
        public void Ctor_AlwaysReadValueFromFile()
        {
            // Arrange
            File.Delete(TelemetryDataRepository.GetStorageFilePath());

            var repository = new TelemetryDataRepository();
            repository.Data.IsAnonymousDataShared = false;
            repository.Data.InstallationDate = DateTime.MaxValue;
            repository.Data.LastSavedAnalysisDate = DateTime.MaxValue;
            repository.Data.NumberOfDaysOfUse = long.MaxValue;
            repository.Data.LastUploadDate = DateTime.MaxValue;
            repository.Save();
            repository.Dispose();

            // Act
            repository = new TelemetryDataRepository();

            // Assert
            repository.Data.IsAnonymousDataShared.Should().BeFalse();
            repository.Data.InstallationDate.Should().Be(DateTime.MaxValue);
            repository.Data.LastSavedAnalysisDate.Should().Be(DateTime.MaxValue);
            repository.Data.NumberOfDaysOfUse.Should().Be(long.MaxValue);
            repository.Data.LastUploadDate.Should().Be(DateTime.MaxValue);
        }

        [TestMethod]
        public void Save_SavesIntoXmlAllValuesOfData()
        {
            // Arrange
            File.Delete(TelemetryDataRepository.GetStorageFilePath());

            var repository = new TelemetryDataRepository();
            repository.Data.IsAnonymousDataShared = false;
            repository.Data.InstallationDate = DateTime.MaxValue;
            repository.Data.LastSavedAnalysisDate = DateTime.MaxValue;
            repository.Data.NumberOfDaysOfUse = long.MaxValue;
            repository.Data.LastUploadDate = DateTime.MaxValue;

            // Act
            repository.Save();

            // Assert
            var stream = File.OpenRead(TelemetryDataRepository.GetStorageFilePath());
            var serializer = new XmlSerializer(typeof(TelemetryData));
            var data = serializer.Deserialize(stream) as TelemetryData;

            data.IsAnonymousDataShared.Should().BeFalse();
            data.InstallationDate.Should().Be(DateTime.MaxValue);
            data.LastSavedAnalysisDate.Should().Be(DateTime.MaxValue);
            data.NumberOfDaysOfUse.Should().Be(long.MaxValue);
            data.LastUploadDate.Should().Be(DateTime.MaxValue);
        }

        [TestMethod]
        public void Instance_AutomaticallyReadFileOnChange()
        {
            // Arrange
            RetryHelper.RetryOnException(5, TimeSpan.FromSeconds(1),
                () => File.Delete(TelemetryDataRepository.GetStorageFilePath()));

            var repository = new TelemetryDataRepository();
            repository.Data.IsAnonymousDataShared = false;
            repository.Data.InstallationDate = DateTime.MaxValue;
            repository.Data.LastSavedAnalysisDate = DateTime.MaxValue;
            repository.Data.NumberOfDaysOfUse = long.MaxValue;
            repository.Data.LastUploadDate = DateTime.MaxValue;

            var otherRepository = new TelemetryDataRepository();

            // Act
            repository.Save();
            Task.Delay(700).Wait();

            // Assert
            otherRepository.Data.InstallationDate.Should().Be(DateTime.MaxValue);
            otherRepository.Data.LastSavedAnalysisDate.Should().Be(DateTime.MaxValue);
            otherRepository.Data.NumberOfDaysOfUse.Should().Be(long.MaxValue);
            otherRepository.Data.LastUploadDate.Should().Be(DateTime.MaxValue);
            otherRepository.Data.IsAnonymousDataShared.Should().BeFalse();
        }
    }
}
