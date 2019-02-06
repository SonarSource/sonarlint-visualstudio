/*
* SonarQube Client
* Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Api
{
    [TestClass]
    public class SonarQubeService_GetRulesAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetRulesAsync_Active_SonarQubeResponse()
        {
            await ConnectToSonarQube();

            SetupRequest("api/rules/search?activation=true&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500", @"
{
  ""total"": 4,
  ""p"": 1,
  ""ps"": 500,
  ""rules"": [
    {
      ""key"": ""csharpsquid:S2225"",
      ""repo"": ""csharpsquid"",
      ""params"": [],
      ""type"": ""BUG""
    },
    {
      ""key"": ""csharpsquid:S4524"",
      ""repo"": ""csharpsquid"",
      ""params"": [],
      ""type"": ""VULNERABILITY""
    },
    {
      ""key"": ""csharpsquid:S2342"",
      ""repo"": ""csharpsquid"",
      ""params"": [
        {
          ""key"": ""format"",
          ""htmlDesc"": ""Regular expression used to check the enumeration type names against."",
          ""defaultValue"": ""^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?$"",
          ""type"": ""STRING""
        },
        {
          ""key"": ""flagsAttributeFormat"",
          ""htmlDesc"": ""Regular expression used to check the flags enumeration type names against."",
          ""defaultValue"": ""^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?s$"",
          ""type"": ""STRING""
        }
      ],
      ""type"": ""CODE_SMELL""
    },
  ],
  ""actives"": {
    ""csharpsquid:S2225"": [
      {
        ""qProfile"": ""quality-profile-1"",
        ""inherit"": ""NONE"",
        ""severity"": ""MAJOR"",
        ""params"": [],
        ""createdAt"": ""2019-01-10T14:00:14+0100"",
        ""updatedAt"": ""2019-01-10T14:00:14+0100""
      }
    ],
    ""csharpsquid:S4524"": [
      {
        ""qProfile"": ""quality-profile-1"",
        ""inherit"": ""NONE"",
        ""severity"": ""CRITICAL"",
        ""params"": [],
        ""createdAt"": ""2019-01-10T14:00:14+0100"",
        ""updatedAt"": ""2019-01-10T14:00:14+0100""
      }
    ],
    ""csharpsquid:S2342"": [
      {
        ""qProfile"": ""quality-profile-1"",
        ""inherit"": ""NONE"",
        ""severity"": ""MINOR"",
        ""params"": [
          {
            ""key"": ""format"",
            ""value"": ""^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?$""
          },
          {
            ""key"": ""flagsAttributeFormat"",
            ""value"": ""^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?s$""
          }
        ],
        ""createdAt"": ""2019-01-10T14:00:14+0100"",
        ""updatedAt"": ""2019-01-10T14:00:14+0100""
      }
    ]
  },
  ""qProfiles"": {
    ""quality-profile-1"": {
      ""name"": ""Sonar way"",
      ""lang"": ""cs"",
      ""langName"": ""C#""
    }
  }
}
");

            var result = await service.GetRulesAsync(true, "quality-profile-1", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);
            result.Select(r => r.Key).Should().Contain(new[] { "S2225", "S4524", "S2342" });
            result.Select(r => r.RepositoryKey).Should().Contain(new[] { "csharpsquid", "csharpsquid", "csharpsquid" });
            result.Select(r => r.IsActive).Should().Contain(new[] { true, true, true });

            result.Select(r => r.Parameters.Count).Should().Contain(new[] { 0, 2, 0 });
            result.SelectMany(r => r.Parameters.Select(p => p.Key)).Should().Contain(new[] { "format", "flagsAttributeFormat" });
            result.SelectMany(r => r.Parameters.Select(p => p.Value)).Should().Contain(new[] { "^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?$", "^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?s$" });
        }

        [TestMethod]
        public async Task GetRulesAsync_NotActive_SonarQubeResponse()
        {
            await ConnectToSonarQube();

            SetupRequest("api/rules/search?activation=false&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500", @"
{
  ""total"": 4,
  ""p"": 1,
  ""ps"": 500,
  ""rules"": [
    {
      ""key"": ""csharpsquid:S2225"",
      ""repo"": ""csharpsquid"",
      ""params"": [],
      ""type"": ""BUG""
    },
    {
      ""key"": ""csharpsquid:S4524"",
      ""repo"": ""csharpsquid"",
      ""params"": [],
      ""type"": ""VULNERABILITY""
    },
    {
      ""key"": ""csharpsquid:S2342"",
      ""repo"": ""csharpsquid"",
      ""params"": [
        {
          ""key"": ""format"",
          ""htmlDesc"": ""Regular expression used to check the enumeration type names against."",
          ""defaultValue"": ""^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?$"",
          ""type"": ""STRING""
        },
        {
          ""key"": ""flagsAttributeFormat"",
          ""htmlDesc"": ""Regular expression used to check the flags enumeration type names against."",
          ""defaultValue"": ""^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?s$"",
          ""type"": ""STRING""
        }
      ],
      ""type"": ""CODE_SMELL""
    },
  ],
  ""actives"": { },
  ""qProfiles"": { }
}
");

            var result = await service.GetRulesAsync(false, "quality-profile-1", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);
            result.Select(r => r.Key).Should().Contain(new[] { "S2225", "S4524", "S2342" });
            result.Select(r => r.RepositoryKey).Should().Contain(new[] { "csharpsquid", "csharpsquid", "csharpsquid" });
            result.Select(r => r.IsActive).Should().Contain(new[] { false, false, false });

            // The response contains parameter "definitions", the Parameters property contains parameter values
            result.Select(r => r.Parameters.Count).Should().Contain(new[] { 0, 0, 0 });
        }

        [TestMethod]
        public void GetRulesAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IList<SonarQubeRule>>> func = async () =>
                await service.GetRulesAsync(true, "whatever", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}
