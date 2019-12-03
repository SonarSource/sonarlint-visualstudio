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
    }
}
