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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection;

[TestClass]
public class ListUserOrganizationsResponseTests
{
    [TestMethod]
    public void Ctor_NoUserOrganizations_SerializeAsExpected()
    {
        var testSubject = new ListUserOrganizationsResponse([]);

        const string expectedString = """
                                      {
                                        "userOrganizations": []
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    public void Ctor_MultipleUserOrganizations_SerializeAsExpected()
    {
        var testSubject = new ListUserOrganizationsResponse(
            [
                new OrganizationDto("myKey", "myName", "myDesc"), 
                new OrganizationDto("myKey2", "myName2", "myDesc2")
            ]);

        const string expectedString = """
                                      {
                                        "userOrganizations": [
                                          {
                                            "key": "myKey",
                                            "name": "myName",
                                            "description": "myDesc"
                                          },
                                          {
                                            "key": "myKey2",
                                            "name": "myName2",
                                            "description": "myDesc2"
                                          }
                                        ]
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
