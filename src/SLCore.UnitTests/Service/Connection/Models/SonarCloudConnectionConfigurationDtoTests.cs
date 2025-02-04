/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection.Models;

[TestClass]
public class SonarCloudConnectionConfigurationDtoTests
{

    [DataRow("id", true, "org", SonarCloudRegion.EU, """
                                                     {
                                                       "organization": "org",
                                                       "region": "EU",
                                                       "connectionId": "id",
                                                       "disableNotification": true
                                                     }
                                                     """)]
    [DataRow("id id id", true, "org", SonarCloudRegion.EU, """
                                                     {
                                                       "organization": "org",
                                                       "region": "EU",
                                                       "connectionId": "id id id",
                                                       "disableNotification": true
                                                     }
                                                     """)]
    [DataRow("id", false, "org", SonarCloudRegion.EU, """
                                                     {
                                                       "organization": "org",
                                                       "region": "EU",
                                                       "connectionId": "id",
                                                       "disableNotification": false
                                                     }
                                                     """)]
    [DataRow("id", true, "org org org", SonarCloudRegion.EU, """
                                                     {
                                                       "organization": "org org org",
                                                       "region": "EU",
                                                       "connectionId": "id",
                                                       "disableNotification": true
                                                     }
                                                     """)]
    [DataRow("id", true, "org", SonarCloudRegion.US, """
                                                     {
                                                       "organization": "org",
                                                       "region": "US",
                                                       "connectionId": "id",
                                                       "disableNotification": true
                                                     }
                                                     """)]
    [DataTestMethod]
    public void Serialize_AsExpected(string id, bool notificationEnabled, string organization, SonarCloudRegion region, string expected) =>
        JsonConvert.SerializeObject(new SonarCloudConnectionConfigurationDto(id, notificationEnabled, organization, region), Formatting.Indented).Should().Be(expected);
}
