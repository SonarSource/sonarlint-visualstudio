﻿/*
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
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection;

[TestClass]
public class GetAllProjectsParamsTests
{
    [TestMethod]
    public void Serialize_WithSonarQubeToken_AsExpected()
    {
        var token =
            Either<TokenDto, UsernamePasswordDto>.CreateLeft(new TokenDto("super-secret-token"));

        var sonarQubeConnection =
            new TransientSonarQubeConnectionDto("http://localhost:9000", token);

        var testSubject = new GetAllProjectsParams(sonarQubeConnection);

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "serverUrl": "http://localhost:9000",
                                          "credentials": {
                                            "token": "super-secret-token"
                                          }
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    public void Serialize_WithSonarQubeUsernamePassword_AsExpected()
    {
        var credentials =
            Either<TokenDto, UsernamePasswordDto>.CreateRight(new UsernamePasswordDto("jeff@thiscompany.com",
                "betwEEn-me-and-U"));

        var sonarQubeConnection =
            new TransientSonarQubeConnectionDto("http://localhost:9000", credentials);

        var testSubject = new GetAllProjectsParams(sonarQubeConnection);

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "serverUrl": "http://localhost:9000",
                                          "credentials": {
                                            "username": "jeff@thiscompany.com",
                                            "password": "betwEEn-me-and-U"
                                          }
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    public void Serialize_WithSonarCloudToken_AsExpected()
    {
        var token =
            Either<TokenDto, UsernamePasswordDto>.CreateLeft(new TokenDto("super-secret-token"));

        var sonarCloudConnection =
            new TransientSonarCloudConnectionDto("my-org", token);

        var testSubject = new GetAllProjectsParams(sonarCloudConnection);

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "organization": "my-org",
                                          "credentials": {
                                            "token": "super-secret-token"
                                          },
                                          "region": "EU"
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    public void Serialize_WithSonarCloudUsernamePassword_AsExpected()
    {
        var credentials =
            Either<TokenDto, UsernamePasswordDto>.CreateRight(new UsernamePasswordDto("jeff@thiscompany.com",
                "betwEEn-me-and-U"));

        var sonarCloudConnection =
            new TransientSonarCloudConnectionDto("my-org", credentials);

        var testSubject = new GetAllProjectsParams(sonarCloudConnection);

        const string expectedString = """
                                      {
                                        "transientConnection": {
                                          "organization": "my-org",
                                          "credentials": {
                                            "username": "jeff@thiscompany.com",
                                            "password": "betwEEn-me-and-U"
                                          },
                                          "region": "EU"
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
