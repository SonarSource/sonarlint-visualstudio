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

using SonarLint.VisualStudio.Core.Telemetry.Legacy;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Configuration;

[TestClass]
public class SlCoreTelemetryMigrationProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SlCoreTelemetryMigrationProvider, ISLCoreTelemetryMigrationProvider>(
            MefTestHelpers.CreateExport<ITelemetryDataRepository>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SlCoreTelemetryMigrationProvider>();
    }

    [TestMethod]
    public void Get_ConvertsOldTelemetryDataToTelemetryMigration()
    {
        var telemetryDataRepository = Substitute.For<ITelemetryDataRepository>();
        telemetryDataRepository.ReadTelemetryData().Returns(new TelemetryData
        {
            IsAnonymousDataShared = true,
            InstallationDateString = "2017-03-15T06:15:42.1234567+01:00",
            NumberOfDaysOfUse = 32
        });
        var slCoreTelemetryMigrationProvider = new SlCoreTelemetryMigrationProvider(telemetryDataRepository);

        var telemetryMigrationDto = slCoreTelemetryMigrationProvider.Get();
        
        var expectedDto = new TelemetryMigrationDto(
            isEnabled: true,
            installTime: new DateTimeOffset(new DateTime(2017, 3, 15, 6, 15, 42, 123, DateTimeKind.Unspecified).AddTicks(4567), TimeSpan.FromHours(1)),
            numUseDays: 32);
        telemetryMigrationDto.Should().BeEquivalentTo(expectedDto);
    }
}
