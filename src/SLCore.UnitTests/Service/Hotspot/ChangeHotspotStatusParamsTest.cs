/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Hotspot;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Hotspot;

[TestClass]
public class ChangeHotspotStatusParamsTest
{
    [TestMethod]
    [DataRow("TO_REVIEW", HotspotStatus.TO_REVIEW)]
    [DataRow("ACKNOWLEDGED", HotspotStatus.ACKNOWLEDGED)]
    [DataRow("FIXED", HotspotStatus.FIXED)]
    [DataRow("SAFE", HotspotStatus.SAFE)]
    public void Serialized_AsExpected(string newStatusString, HotspotStatus newStatus)
    {
        var expected = $$"""
                         {
                           "configurationScopeId": "CONFIG_SCOPE_ID",
                           "hotspotKey": "hotspotKey",
                           "newStatus": "{{newStatusString}}"
                         }
                         """;

        var changeIssueStatusParams = new ChangeHotspotStatusParams("CONFIG_SCOPE_ID", "hotspotKey", newStatus);

        JsonConvert.SerializeObject(changeIssueStatusParams, Formatting.Indented).Should().Be(expected);
    }
}
