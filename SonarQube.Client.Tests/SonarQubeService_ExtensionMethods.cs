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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_ExtensionMethods : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetAllRulesAsync_FetchesActiveAndInactive()
        {
            await ConnectToSonarQube();

            // One active rule
            SetupRequest("api/rules/search?activation=true&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500", @"
{
  ""total"": 1,
  ""p"": 1,
  ""ps"": 500,
  ""rules"": [
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

            // One inactive rule
            SetupRequest("api/rules/search?activation=false&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500", @"
{
  ""total"": 1,
  ""p"": 1,
  ""ps"": 500,
  ""rules"": [
    {
      ""key"": ""csharpsquid:S2225"",
      ""repo"": ""csharpsquid"",
      ""params"": [],
      ""type"": ""BUG""
    }
  ],
  ""actives"": { },
  ""qProfiles"": { }
}
");


            var result = await service.GetAllRulesAsync("quality-profile-1", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(2);
            result.Select(r => r.Key).Should().Contain(new[] { "S2225", "S2342" });
            result.Select(r => r.RepositoryKey).Should().Contain(new[] { "csharpsquid", "csharpsquid"});
            
            // Active rules should be returned first
            result.Select(r => r.IsActive).Should().Contain(new[] { true, false });

            result.Select(r => r.Parameters.Count).Should().Contain(new[] { 2, 0 });
            result.SelectMany(r => r.Parameters.Select(p => p.Key)).Should().Contain(new[] { "format", "flagsAttributeFormat" });
            result.SelectMany(r => r.Parameters.Select(p => p.Value)).Should().Contain(new[] { "^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?$", "^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?s$" });
        }

        [TestMethod]
        public async Task GetAllRulesAsync_OnlyActiveRulesExist_AreFetched()
        {
            await ConnectToSonarQube();

            // One active rule, no inactive rules
            var ruleJson = SingleValidRuleJson("repo1", "rule1");
            SetupRequest("api/rules/search?activation=true&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500",
                ruleJson);

            SetupRequest("api/rules/search?activation=false&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500",
                NoRulesJson);

            var result = await service.GetAllRulesAsync("quality-profile-1", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(1);
            result.First().Key.Should().Be("rule1");
            result.First().RepositoryKey.Should().Be("repo1");
        }

        [TestMethod]
        public async Task GetAllRulesAsync_OnlyInactiveRulesExist_AreFetched()
        {
            await ConnectToSonarQube();

            // One active rule, no inactive rules
            var ruleJson = SingleValidRuleJson("repo1", "rule1");
            SetupRequest("api/rules/search?activation=true&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500",
                NoRulesJson);

            SetupRequest("api/rules/search?activation=false&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500",
                ruleJson);

            var result = await service.GetAllRulesAsync("quality-profile-1", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(1);
            result.First().Key.Should().Be("rule1");
            result.First().RepositoryKey.Should().Be("repo1");
        }


        [TestMethod]
        public async Task GetAllRulesAsync_GetInactiveFails_ExceptionIsPropogated()
        {
            // Arrange
            await ConnectToSonarQube();

            // One active rule, no inactive rules
            var ruleJson = SingleValidRuleJson("repo1", "rule1");
            SetupRequest("api/rules/search?activation=true&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500",
                ruleJson);

            SetupRequestWithOperation("api/rules/search?activation=false&qprofile=quality-profile-1&f=repo%2CinternalKey%2Cparams%2Cactives&p=1&ps=500",
                () => { throw new InvalidOperationException("xxx"); });

            // Act
            Func<Task> act = async () => await service.GetAllRulesAsync("quality-profile-1", CancellationToken.None);

            // Assert
            act.Should().ThrowExactly<InvalidOperationException>().And.Message.Should().Be("xxx");
        }

        private const string NoRulesJson = @"
{
  'total': 0,
  'p': 1,
  'ps': 500,
  'rules': [],
  'actives': { },
  'qProfiles': { }
}
";

        private static string SingleValidRuleJson(string repoId, string ruleId) =>
@"
{
  'total': 1,
  'p': 1,
  'ps': 500,
  'rules': [
    {
      'key': '***RULE_ID***',
      'repo': '***REPO_ID***',
      'params': [],
      'type': 'BUG'
    }
  ],
  'actives': { },
  'qProfiles': { }
}"
.Replace("***REPO_ID***", repoId)
.Replace("***RULE_ID***", ruleId);

    }
}
