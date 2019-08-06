/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryHelper_Serialize
    {
        [TestMethod]
        public void SerializeLanguages()
        {
            // Check serialization produces json in the expected format
            var payload = new TelemetryPayload
            {
                SonarLintProduct = "my product",
                SonarLintVersion = "1.2.3.4",
                VisualStudioVersion = "15.0.1.2",
                NumberOfDaysSinceInstallation = 234,
                NumberOfDaysOfUse = 123,
                IsUsingConnectedMode = true,
                IsUsingSonarCloud = true,

                // Adding some ticks to ensure that we send just the milliseconds in the serialized payload
                InstallDate = new DateTimeOffset(2017, 12, 23, 8, 25, 35, 456, TimeSpan.FromHours(1)).AddTicks(123),
                SystemDate = new DateTimeOffset(2018, 3, 15, 18, 55, 10, 123, TimeSpan.FromHours(1)).AddTicks(123),

                Analyses = new []
                {
                    new Analysis { Language = "js" },
                    new Analysis { Language = "csharp" },
                    new Analysis { Language = "vbnet" }
                }.ToList()
            };

            var serialized = TelemetryHelper.Serialize(payload);

            var expected = @"{
  ""sonarlint_product"": ""my product"",
  ""sonarlint_version"": ""1.2.3.4"",
  ""ide_version"": ""15.0.1.2"",
  ""days_since_installation"": 234,
  ""days_of_use"": 123,
  ""connected_mode_used"": true,
  ""connected_mode_sonarcloud"": true,
  ""install_time"": ""2017-12-23T08:25:35.456+01:00"",
  ""system_time"": ""2018-03-15T18:55:10.123+01:00"",
  ""analyses"": [
    {
      ""language"": ""js""
    },
    {
      ""language"": ""csharp""
    },
    {
      ""language"": ""vbnet""
    }
  ]
}";
            serialized.Should().Be(expected);
        }
    }
}
