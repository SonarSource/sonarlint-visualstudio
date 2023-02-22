﻿/*
 * SonarQube Client
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

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetHotspotAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetHotspot_Response_From_SonarQube()
        {
            const string hotspotKey = "AW9mgJw6eFC3pGl94Wrf";
            await ConnectToSonarQube("8.1.0.0");

            SetupRequest($"api/hotspots/show?hotspot={hotspotKey}", @"{
  ""key"": ""AW9mgJw6eFC3pGl94Wrf"",
  ""component"": {
    ""organization"": ""default-organization"",
    ""key"": ""com.sonarsource:test-project:src/main/java/com/sonarsource/FourthClass.java"",
    ""qualifier"": ""FIL"",
    ""name"": ""FourthClass.java"",
    ""longName"": ""src/main/java/com/sonarsource/FourthClass.java"",
    ""path"": ""src/main/java/com/sonarsource/FourthClass.java""
  },
  ""project"": {
    ""organization"": ""default-organization"",
    ""key"": ""com.sonarsource:test-project"",
    ""qualifier"": ""TRK"",
    ""name"": ""test-project"",
    ""longName"": ""test-project""
  },
  ""rule"": {
    ""key"": ""java:S4787"",
    ""name"": ""rule-name"",
    ""securityCategory"": ""others"",
    ""vulnerabilityProbability"": ""LOW""
  },
  ""status"": ""TO_REVIEW"",
  ""line"": 10,
  ""message"": ""message"",
  ""assignee"": ""joe"",
  ""author"": ""joe"",
  ""creationDate"": ""2020-01-02T15:43:10+0100"",
  ""updateDate"": ""2020-01-02T15:43:10+0100"",
  ""changelog"": [
    {
      ""user"": ""joe"",
      ""userName"": ""Joe"",
      ""creationDate"": ""2020-01-02T14:44:55+0100"",
      ""diffs"": [
        {
          ""key"": ""diff-key-0"",
          ""newValue"": ""new-value-0"",
          ""oldValue"": ""old-value-0""
        }
      ],
      ""avatar"": ""my-avatar"",
      ""isUserActive"": true
    },
    {
      ""user"": ""joe"",
      ""userName"": ""Joe"",
      ""creationDate"": ""2020-01-02T14:44:55+0100"",
      ""diffs"": [
        {
          ""key"": ""diff-key-1"",
          ""newValue"": ""new-value-1"",
          ""oldValue"": ""old-value-1""
        }
      ],
      ""avatar"": ""my-avatar"",
      ""isUserActive"": true
    },
    {
      ""user"": ""joe"",
      ""userName"": ""Joe"",
      ""creationDate"": ""2020-01-02T14:44:55+0100"",
      ""diffs"": [
        {
          ""key"": ""diff-key-2"",
          ""newValue"": ""new-value-2"",
          ""oldValue"": ""old-value-2""
        }
      ],
      ""avatar"": ""my-avatar"",
      ""isUserActive"": true
    }
  ],
  ""comment"": [
    {
      ""key"": ""comment-0"",
      ""login"": ""Joe"",
      ""htmlText"": ""html text 0"",
      ""markdown"": ""markdown 0"",
      ""createdAt"": ""2020-01-02T14:47:47+0100""
    },
    {
      ""key"": ""comment-1"",
      ""login"": ""Joe"",
      ""htmlText"": ""html text 1"",
      ""markdown"": ""markdown 1"",
      ""createdAt"": ""2020-01-02T14:47:47+0100""
    },
    {
      ""key"": ""comment-2"",
      ""login"": ""Joe"",
      ""htmlText"": ""html text 2"",
      ""markdown"": ""markdown 2"",
      ""createdAt"": ""2020-01-02T14:47:47+0100""
    }
  ],
  ""users"": [
    {
      ""login"": ""joe"",
      ""name"": ""Joe"",
      ""active"": true
    }
  ],
  ""canChangeStatus"": true
}");

            var result = await service.GetHotspotAsync(hotspotKey, CancellationToken.None);

            messageHandler.VerifyAll();

            result.HotspotKey.Should().Be(hotspotKey);
            result.Message.Should().Be("message");
            result.Assignee.Should().Be("joe");
            result.Status.Should().Be("TO_REVIEW");
            result.Line.Should().Be(10);

            result.Organization.Should().Be("default-organization");
            result.ProjectKey.Should().Be("com.sonarsource:test-project");
            result.ProjectName.Should().Be("test-project");

            result.ComponentKey.Should().Be("com.sonarsource:test-project:src/main/java/com/sonarsource/FourthClass.java");
            result.ComponentPath.Should().Be("src/main/java/com/sonarsource/FourthClass.java");

            result.RuleKey.Should().Be("java:S4787");
            result.RuleName.Should().Be("rule-name");
            result.SecurityCategory.Should().Be("others");
            result.VulnerabilityProbability.Should().Be("LOW");
        }

        [TestMethod]
        public void GetHotspotAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<SonarQubeHotspot>> func = async () =>
                await service.GetHotspotAsync(It.IsAny<string>(), CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}
