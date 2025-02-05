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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection.Models;

[TestClass]
public class TransientSonarCloudConnectionDtoTests
{
    [DataRow("my org", SonarCloudRegion.US, """
                                            {
                                              "organization": "my org",
                                              "credentials": {
                                                "token": "abctoken"
                                              },
                                              "region": "US"
                                            }
                                            """)]
    [DataRow("my org two", SonarCloudRegion.EU, """
                                                {
                                                  "organization": "my org two",
                                                  "credentials": {
                                                    "token": "abctoken"
                                                  },
                                                  "region": "EU"
                                                }
                                                """)]
    [DataTestMethod]
    public void Serialized_Token_AsExpected(string organization, SonarCloudRegion region, string expected) =>
        JsonConvert.SerializeObject(new TransientSonarCloudConnectionDto(organization, new TokenDto("abctoken"), region), Formatting.Indented).Should().Be(expected);

    [DataRow("my org", SonarCloudRegion.US, """
                                            {
                                              "organization": "my org",
                                              "credentials": {
                                                "username": "usr",
                                                "password": "pwd"
                                              },
                                              "region": "US"
                                            }
                                            """)]
    [DataRow("my org two", SonarCloudRegion.EU, """
                                                {
                                                  "organization": "my org two",
                                                  "credentials": {
                                                    "username": "usr",
                                                    "password": "pwd"
                                                  },
                                                  "region": "EU"
                                                }
                                                """)]
    [DataTestMethod]
    public void Serialized_UsernameAndPassword_AsExpected(string organization, SonarCloudRegion region, string expected) =>
        JsonConvert.SerializeObject(new TransientSonarCloudConnectionDto(organization, new UsernamePasswordDto("usr", "pwd"), region), Formatting.Indented).Should().Be(expected);
}
