/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetRuleByKeyAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetRuleByKeyAsync_RuleIsFound_ReturnsRule()
        {
            await ConnectToSonarQube("10.2.0.0");

            SetupRequest(
                "api/rules/search?qprofile=qpKey&rule_key=csharpsquid%3AS2342&f=repo%2CinternalKey%2Cparams%2Cactives%2ChtmlDesc%2Ctags%2Cname%2ChtmlNote%2CdescriptionSections%2CeducationPrinciples%2CcleanCodeAttribute%2Cimpacts&p=1&ps=500",
                @"
{
  ""total"": 1,
  ""p"": 1,
  ""ps"": 500,
  ""rules"": [
    {
      ""key"": ""csharpsquid:S2342"",
      ""repo"": ""csharpsquid"",
      ""htmlDesc"": ""Html Description"",
      ""htmlNote"": ""HTML Note"",
      ""name"": ""RuleName"",
      ""tags"": [""tag1"",""tag2""],
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
      ""type"": ""CODE_SMELL"",
      ""cleanCodeAttributeCategory"": ""INTENTIONAL"",
    ""cleanCodeAttribute"": ""CLEAR"",
      ""impacts"": [
                {
                    ""softwareQuality"": ""RELIABILITY"",
                    ""severity"": ""MEDIUM""
                }
            ],
      ""descriptionSections"" : [
        {
          ""key"": ""key1"",
          ""content"": ""content1""
        },
        {
          ""key"": ""key2"",
          ""content"": ""content2"",
          ""context"":{
            ""displayName"":""displayName"",
            ""key"":""key""
          }
        }
        ],
         ""educationPrinciples"": [
            ""education principle 1"",
            ""education principle 2""
        ]
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

            var result = await service.GetRuleByKeyAsync("csharpsquid:S2342", "qpKey", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();

            result.Key.Should().Be("S2342");
            result.RepositoryKey.Should().Be("csharpsquid");
            result.Description.Should().Be("Html Description");
            result.HtmlNote.Should().Be("HTML Note");
            result.Severity.Should().Be(SonarQubeIssueSeverity.Minor);
            result.Name.Should().Be("RuleName");
            result.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
            result.CleanCodeAttribute.Should().Be(SonarQubeCleanCodeAttribute.Clear);
            result.SoftwareQualitySeverities.Should().BeEquivalentTo(
                new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity>
                    { { SonarQubeSoftwareQuality.Reliability, SonarQubeSoftwareQualitySeverity.Medium } });

            result.DescriptionSections.Count.Should().Be(2);
            result.DescriptionSections[0].Key.Should().Be("key1");
            result.DescriptionSections[0].HtmlContent.Should().Be("content1");
            result.DescriptionSections[0].Context.Should().BeNull();

            result.DescriptionSections[1].Key.Should().Be("key2");
            result.DescriptionSections[1].HtmlContent.Should().Be("content2");
            result.DescriptionSections[1].Context.Should().NotBeNull();
            result.DescriptionSections[1].Context.Key.Should().Be("key");
            result.DescriptionSections[1].Context.DisplayName.Should().Be("displayName");
        }

        [TestMethod]
        public async Task GetRuleByKeyAsync_RuleIsNotFound_ReturnsNull()
        {
            await ConnectToSonarQube();

            SetupRequest("api/rules/search?qprofile=qpKey&rule_key=csharpsquid%3AS2342XX&f=repo%2CinternalKey%2Cparams%2Cactives%2ChtmlDesc%2Ctags%2Cname%2ChtmlNote&p=1&ps=500", @"{
    ""total"": 0,
    ""p"": 1,
    ""ps"": 100,
    ""rules"": [],
    ""actives"": {},
    ""qProfiles"": {},
    ""paging"": {
        ""pageIndex"": 1,
        ""pageSize"": 100,
        ""total"": 0
    }
}
");

            var result = await service.GetRuleByKeyAsync("csharpsquid:S2342XX", "qpKey", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().BeNull();
        }

        [TestMethod]
        public void GetRuleByKeyAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<SonarQubeRule>> func = async () =>
                await service.GetRuleByKeyAsync("whatever", "qpKey", CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}
