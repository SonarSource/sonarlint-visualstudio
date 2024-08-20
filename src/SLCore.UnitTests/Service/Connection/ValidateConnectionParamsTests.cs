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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection;

[TestClass]
public class ValidateConnectionParamsTests
{
    [TestMethod]
    public void Ctor_TransientSonarQubeConnectionDtoWithCredentials_SerializeAsExpected()
    {
        var testSubject = new ValidateConnectionParams(new TransientSonarQubeConnectionDto("http://localhost:9000", Either<TokenDto, UsernamePasswordDto>.CreateRight(new UsernamePasswordDto("myUser", "password"))));

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "serverUrl": "http://localhost:9000",
                                          "credentials": {
                                            "username": "myUser",
                                            "password": "password"
                                          }
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    public void Ctor_TransientSonarQubeConnectionDtoWithToken_SerializeAsExpected()
    {
        var testSubject = new ValidateConnectionParams(new TransientSonarQubeConnectionDto("http://localhost:9000", Either<TokenDto, UsernamePasswordDto>.CreateLeft(new TokenDto("myToken"))));

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "serverUrl": "http://localhost:9000",
                                          "credentials": {
                                            "token": "myToken"
                                          }
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }


    [TestMethod]
    public void Ctor_TransientSonarCloudConnectionDtoWithCredentials_SerializeAsExpected()
    {
        var testSubject = new ValidateConnectionParams(new TransientSonarCloudConnectionDto("myOrg", Either<TokenDto, UsernamePasswordDto>.CreateRight(new UsernamePasswordDto("myUser", "password"))));

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "organization": "myOrg",
                                          "credentials": {
                                            "username": "myUser",
                                            "password": "password"
                                          }
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    public void Ctor_TransientSonarCloudConnectionDtoDtoWithToken_SerializeAsExpected()
    {
        var testSubject = new ValidateConnectionParams(new TransientSonarCloudConnectionDto("myOrg", Either<TokenDto, UsernamePasswordDto>.CreateLeft(new TokenDto("myToken"))));

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "organization": "myOrg",
                                          "credentials": {
                                            "token": "myToken"
                                          }
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
