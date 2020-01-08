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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryDataTests
    {
        [TestMethod]
        public void XmlSerialization_RoundTrips()
        {
            var telemetrySerializer = new XmlSerializer(typeof(TelemetryData));

            var originalData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                NumberOfDaysOfUse = 999,

                // Not serialized directly: converted then saved
                InstallationDate = DateTimeOffset.UtcNow - TimeSpan.FromDays(100),
                LastSavedAnalysisDate = DateTimeOffset.UtcNow - TimeSpan.FromHours(200),
                LastUploadDate = DateTimeOffset.UtcNow - -TimeSpan.FromMinutes(300),

                Analyses = new Analysis[]
                    {
                        new Analysis { Language = "js" },
                        new Analysis { Language = "csharp" },
                        new Analysis { Language = "xxx" }
                    }.ToList()
            };

            string serializedData = null;
            using (var textWriter = new StringWriter())
            {
                telemetrySerializer.Serialize(textWriter, originalData);
                textWriter.Flush();
                serializedData = textWriter.ToString();
            }

            TelemetryData reloadedData = null;
            using (var textReader = new StringReader(serializedData))
            {
                reloadedData = telemetrySerializer.Deserialize(textReader) as TelemetryData;
            }

            reloadedData.IsAnonymousDataShared.Should().BeTrue();
            reloadedData.NumberOfDaysOfUse.Should().Be(999);

            reloadedData.InstallationDate.Should().Be(originalData.InstallationDate);
            reloadedData.LastSavedAnalysisDate.Should().Be(originalData.LastSavedAnalysisDate);
            reloadedData.LastUploadDate.Should().Be(originalData.LastUploadDate);

            reloadedData.Analyses.Count.Should().Be(3);
            reloadedData.Analyses[0].Language.Should().Be("js");
            reloadedData.Analyses[1].Language.Should().Be("csharp");
            reloadedData.Analyses[2].Language.Should().Be("xxx");
        }
    }
}
