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

using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry.Legacy;
using SonarLint.VisualStudio.Integration.Telemetry.Legacy;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry.Legacy;

[TestClass]
public class TelemetryDataRepositoryTests
{
    [TestMethod]
    public void Data_WhenTelemetryFileDoesNotExist_ReturnNull()
    {
        var environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
        var fileSystem = CreateFileSystem(environmentVariableProvider, false);
        var telemetryDataRepository = new TelemetryDataRepository(fileSystem, environmentVariableProvider);
        
        var actualData = telemetryDataRepository.ReadTelemetryData();

        actualData.Should().BeNull();
    }

    [TestMethod]
    public void Data_WhenTelemetryFileExist_ShouldProvideTelemetryData()
    {
        const string fileContent = """
                                    <?xml version="1.0" encoding="utf-16"?>
                                    <TelemetryData  xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                                      <IsAnonymousDataShared>false</IsAnonymousDataShared>
                                      <NumberOfDaysOfUse>10</NumberOfDaysOfUse>
                                      <InstallationDate>2010-03-15T06:15:42.1234567+01:00</InstallationDate>
                                    </TelemetryData>
                                    """;
        var environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
        var fileSystem = CreateFileSystem(environmentVariableProvider, true);
        fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);
        var telemetryDataRepository = new TelemetryDataRepository(fileSystem, environmentVariableProvider);

        var actualData = telemetryDataRepository.ReadTelemetryData();
        
        var expectedData = new TelemetryData
        {
            IsAnonymousDataShared = false,
            InstallationDateString = "2010-03-15T06:15:42.1234567+01:00",
            NumberOfDaysOfUse = 10
        };
        actualData.Should().BeEquivalentTo(expectedData);
    }

    [TestMethod]
    public void Data_WhenTelemetryFileIsCorrupted_ShouldDeleteFileAndReturnNull()
    {
        var environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
        var fileSystem = CreateFileSystem(environmentVariableProvider, true);
        var telemetryDataRepository = new TelemetryDataRepository(fileSystem, environmentVariableProvider);

        var actualData = telemetryDataRepository.ReadTelemetryData();

        fileSystem.File.Received().Delete(Arg.Any<string>());
        actualData.Should().BeNull();
    }

    private static IFileSystem CreateFileSystem(IEnvironmentVariableProvider environmentVariableProvider, bool fileExists)
    {
        var expectedFilePath = TelemetryDataRepository.GetStorageFilePath(environmentVariableProvider);
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.File.Exists(expectedFilePath).Returns(fileExists);
        return fileSystem;
    }
}
