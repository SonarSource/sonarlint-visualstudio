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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Api
{
    [TestClass]
    public class SonarQubeService_GetQualityProfileAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetQualityProfile_Old_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("api/qualityprofiles/search?projectKey=my_project&organization=my_organization", @"{
  ""profiles"": [
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL3"",
      ""name"": ""Sonar way"",
      ""language"": ""cs"",
      ""languageName"": ""C#"",
      ""isInherited"": false,
      ""isBuiltIn"": true,
      ""activeRuleCount"": 3,
      ""activeDeprecatedRuleCount"": 0,
      ""isDefault"": true,
      ""ruleUpdatedAt"": ""2016-12-22T19:10:03+0100"",
      ""lastUsed"": ""2016-12-01T19:10:03+0100"",
      ""actions"": {
        ""edit"": false,
        ""setAsDefault"": false,
        ""copy"": false
      }
    },
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL1"",
      ""name"": ""My BU Profile"",
      ""language"": ""java"",
      ""languageName"": ""Java"",
      ""isInherited"": true,
      ""isBuiltIn"": false,
      ""parentKey"": ""iU5OvuD2FLz"",
      ""parentName"": ""My Company Profile"",
      ""activeRuleCount"": 15,
      ""activeDeprecatedRuleCount"": 5,
      ""isDefault"": false,
      ""projectCount"": 7,
      ""ruleUpdatedAt"": ""2016-12-20T19:10:03+0100"",
      ""lastUsed"": ""2016-12-21T16:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-28T21:57:01+0200"",
      ""actions"": {
        ""edit"": true,
        ""setAsDefault"": false,
        ""copy"": false
      }
    },
    {
      ""key"": ""iU5OvuD2FLz"",
      ""name"": ""My Company Profile"",
      ""language"": ""java"",
      ""languageName"": ""Java"",
      ""isInherited"": false,
      ""isDefault"": true,
      ""isBuiltIn"": false,
      ""activeRuleCount"": 9,
      ""activeDeprecatedRuleCount"": 2,
      ""ruleUpdatedAt"": ""2016-12-22T19:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-29T21:57:01+0200"",
      ""actions"": {
        ""edit"": true,
        ""setAsDefault"": false,
        ""copy"": false
      }
    },
    {
      ""key"": ""AU-TpxcB-iU5OvuD2FL7"",
      ""name"": ""Sonar way"",
      ""language"": ""py"",
      ""languageName"": ""Python"",
      ""isInherited"": false,
      ""isBuiltIn"": true,
      ""activeRuleCount"": 2,
      ""activeDeprecatedRuleCount"": 0,
      ""isDefault"": true,
      ""ruleUpdatedAt"": ""2014-12-22T19:10:03+0100"",
      ""actions"": {
        ""edit"": false,
        ""setAsDefault"": false,
        ""copy"": false
      }
    }
  ],
  ""actions"": {
    ""create"": false
  }
}");

            SetupRequest("api/qualityprofiles/changelog?profileKey=AU-TpxcA-iU5OvuD2FL3&p=1&ps=1", @"{
  ""total"": 3,
  ""ps"": 10,
  ""p"": 1,
  ""events"": [
    {
      ""date"" : ""2015-02-23T17:58:39+0100"",
      ""action"" : ""ACTIVATED"",
      ""authorLogin"" : ""anakin.skywalker"",
      ""authorName"" : ""Anakin Skywalker"",
      ""ruleKey"" : ""squid:S2438"",
      ""ruleName"" : ""\""Threads\"" should not be used where \""Runnables\"" are expected"",
      ""params"" : {
        ""severity"" : ""CRITICAL""
      }
    }
  ]
}");

            var result = await service.GetQualityProfileAsync("my_project", "my_organization", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
            result.IsDefault.Should().BeTrue();
            result.Key.Should().Be("AU-TpxcA-iU5OvuD2FL3");
            result.Language.Should().Be("cs");
            result.Name.Should().Be("Sonar way");
            result.TimeStamp.Should().Be(DateTime.Parse("2015-02-23T17:58:39+0100"));
        }

        [TestMethod]
        public async Task GetQualityProfile_Old_ProjectNotAnalyzed()
        {
            await ConnectToSonarQube();

            SetupRequest("api/qualityprofiles/search?projectKey=my_project&organization=my_organization", "",
                HttpStatusCode.NotFound);

            SetupRequest("api/qualityprofiles/search?organization=my_organization&defaults=true", @"{
  ""profiles"": [
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL3"",
      ""name"": ""Sonar way"",
      ""language"": ""cs"",
      ""languageName"": ""C#"",
      ""isInherited"": false,
      ""isBuiltIn"": true,
      ""activeRuleCount"": 3,
      ""activeDeprecatedRuleCount"": 0,
      ""isDefault"": true,
      ""ruleUpdatedAt"": ""2016-12-22T19:10:03+0100"",
      ""lastUsed"": ""2016-12-01T19:10:03+0100"",
      ""actions"": {
        ""edit"": false,
        ""setAsDefault"": false,
        ""copy"": false
      }
    }
  ],
  ""actions"": {
    ""create"": false
  }
}");

            SetupRequest("api/qualityprofiles/changelog?profileKey=AU-TpxcA-iU5OvuD2FL3&p=1&ps=1", @"{
  ""total"": 3,
  ""ps"": 10,
  ""p"": 1,
  ""events"": [
    {
      ""date"" : ""2015-02-23T17:58:39+0100"",
      ""action"" : ""ACTIVATED"",
      ""authorLogin"" : ""anakin.skywalker"",
      ""authorName"" : ""Anakin Skywalker"",
      ""ruleKey"" : ""squid:S2438"",
      ""ruleName"" : ""\""Threads\"" should not be used where \""Runnables\"" are expected"",
      ""params"" : {
        ""severity"" : ""CRITICAL""
      }
    }
  ]
}");

            var result = await service.GetQualityProfileAsync("my_project", "my_organization", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
            result.IsDefault.Should().BeTrue();
            result.Key.Should().Be("AU-TpxcA-iU5OvuD2FL3");
            result.Language.Should().Be("cs");
            result.Name.Should().Be("Sonar way");
            result.TimeStamp.Should().Be(DateTime.Parse("2015-02-23T17:58:39+0100"));
        }

        [TestMethod]
        public async Task GetQualityProfile_Old_Error()
        {
            await ConnectToSonarQube();

            SetupRequest("api/qualityprofiles/search?projectKey=my_project&organization=my_organization", "",
                HttpStatusCode.InternalServerError);

            Func<Task<SonarQubeQualityProfile>> func = async () =>
                await service.GetQualityProfileAsync("my_project", "my_organization", SonarQubeLanguage.CSharp, CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 500 (Internal Server Error).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetQualityProfile_New_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("6.5.0.0");

            SetupRequest("api/qualityprofiles/search?project=my_project&organization=my_organization", @"{
  ""profiles"": [
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL3"",
      ""name"": ""Sonar way"",
      ""language"": ""cs"",
      ""languageName"": ""C#"",
      ""isInherited"": false,
      ""isBuiltIn"": true,
      ""activeRuleCount"": 3,
      ""activeDeprecatedRuleCount"": 0,
      ""isDefault"": true,
      ""ruleUpdatedAt"": ""2016-12-22T19:10:03+0100"",
      ""lastUsed"": ""2016-12-01T19:10:03+0100"",
      ""actions"": {
        ""edit"": false,
        ""setAsDefault"": false,
        ""copy"": false
      }
    },
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL1"",
      ""name"": ""My BU Profile"",
      ""language"": ""java"",
      ""languageName"": ""Java"",
      ""isInherited"": true,
      ""isBuiltIn"": false,
      ""parentKey"": ""iU5OvuD2FLz"",
      ""parentName"": ""My Company Profile"",
      ""activeRuleCount"": 15,
      ""activeDeprecatedRuleCount"": 5,
      ""isDefault"": false,
      ""projectCount"": 7,
      ""ruleUpdatedAt"": ""2016-12-20T19:10:03+0100"",
      ""lastUsed"": ""2016-12-21T16:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-28T21:57:01+0200"",
      ""actions"": {
        ""edit"": true,
        ""setAsDefault"": false,
        ""copy"": false
      }
    },
    {
      ""key"": ""iU5OvuD2FLz"",
      ""name"": ""My Company Profile"",
      ""language"": ""java"",
      ""languageName"": ""Java"",
      ""isInherited"": false,
      ""isDefault"": true,
      ""isBuiltIn"": false,
      ""activeRuleCount"": 9,
      ""activeDeprecatedRuleCount"": 2,
      ""ruleUpdatedAt"": ""2016-12-22T19:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-29T21:57:01+0200"",
      ""actions"": {
        ""edit"": true,
        ""setAsDefault"": false,
        ""copy"": false
      }
    },
    {
      ""key"": ""AU-TpxcB-iU5OvuD2FL7"",
      ""name"": ""Sonar way"",
      ""language"": ""py"",
      ""languageName"": ""Python"",
      ""isInherited"": false,
      ""isBuiltIn"": true,
      ""activeRuleCount"": 2,
      ""activeDeprecatedRuleCount"": 0,
      ""isDefault"": true,
      ""ruleUpdatedAt"": ""2014-12-22T19:10:03+0100"",
      ""actions"": {
        ""edit"": false,
        ""setAsDefault"": false,
        ""copy"": false
      }
    }
  ],
  ""actions"": {
    ""create"": false
  }
}");

            SetupRequest("api/qualityprofiles/changelog?qualityProfile=Sonar+way&language=cs&p=1&ps=1", @"{
  ""total"": 3,
  ""ps"": 10,
  ""p"": 1,
  ""events"": [
    {
      ""date"" : ""2015-02-23T17:58:39+0100"",
      ""action"" : ""ACTIVATED"",
      ""authorLogin"" : ""anakin.skywalker"",
      ""authorName"" : ""Anakin Skywalker"",
      ""ruleKey"" : ""squid:S2438"",
      ""ruleName"" : ""\""Threads\"" should not be used where \""Runnables\"" are expected"",
      ""params"" : {
        ""severity"" : ""CRITICAL""
      }
    }
  ]
}");

            var result = await service.GetQualityProfileAsync("my_project", "my_organization", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
            result.IsDefault.Should().BeTrue();
            result.Key.Should().Be("AU-TpxcA-iU5OvuD2FL3");
            result.Language.Should().Be("cs");
            result.Name.Should().Be("Sonar way");
            result.TimeStamp.Should().Be(DateTime.Parse("2015-02-23T17:58:39+0100"));
        }

        [TestMethod]
        public async Task GetQualityProfile_New_ProjectNotAnalyzed()
        {
            await ConnectToSonarQube("6.5.0.0");

            SetupRequest("api/qualityprofiles/search?project=my_project&organization=my_organization", "",
                HttpStatusCode.NotFound);

            SetupRequest("api/qualityprofiles/search?organization=my_organization&defaults=true", @"{
  ""profiles"": [
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL3"",
      ""name"": ""Sonar way"",
      ""language"": ""cs"",
      ""languageName"": ""C#"",
      ""isInherited"": false,
      ""isBuiltIn"": true,
      ""activeRuleCount"": 3,
      ""activeDeprecatedRuleCount"": 0,
      ""isDefault"": true,
      ""ruleUpdatedAt"": ""2016-12-22T19:10:03+0100"",
      ""lastUsed"": ""2016-12-01T19:10:03+0100"",
      ""actions"": {
        ""edit"": false,
        ""setAsDefault"": false,
        ""copy"": false
      }
    }
  ],
  ""actions"": {
    ""create"": false
  }
}");

            SetupRequest("api/qualityprofiles/changelog?qualityProfile=Sonar+way&language=cs&p=1&ps=1", @"{
  ""total"": 3,
  ""ps"": 10,
  ""p"": 1,
  ""events"": [
    {
      ""date"" : ""2015-02-23T17:58:39+0100"",
      ""action"" : ""ACTIVATED"",
      ""authorLogin"" : ""anakin.skywalker"",
      ""authorName"" : ""Anakin Skywalker"",
      ""ruleKey"" : ""squid:S2438"",
      ""ruleName"" : ""\""Threads\"" should not be used where \""Runnables\"" are expected"",
      ""params"" : {
        ""severity"" : ""CRITICAL""
      }
    }
  ]
}");

            var result = await service.GetQualityProfileAsync("my_project", "my_organization", SonarQubeLanguage.CSharp,
                CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().NotBeNull();
            result.IsDefault.Should().BeTrue();
            result.Key.Should().Be("AU-TpxcA-iU5OvuD2FL3");
            result.Language.Should().Be("cs");
            result.Name.Should().Be("Sonar way");
            result.TimeStamp.Should().Be(DateTime.Parse("2015-02-23T17:58:39+0100"));
        }

        [TestMethod]
        public async Task GetQualityProfile_New_NoQualityProfile_Eg_NoSonarCSharpInstalled()
        {
            await ConnectToSonarQube("6.5.0.0");

            // Only a java Quality Profile is returned
            SetupRequest("api/qualityprofiles/search?project=my_project&organization=my_organization", @"{
  ""profiles"": [
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL1"",
      ""name"": ""My BU Profile"",
      ""language"": ""java"",
      ""languageName"": ""Java"",
      ""isInherited"": true,
      ""isBuiltIn"": false,
      ""parentKey"": ""iU5OvuD2FLz"",
      ""parentName"": ""My Company Profile"",
      ""activeRuleCount"": 15,
      ""activeDeprecatedRuleCount"": 5,
      ""isDefault"": false,
      ""projectCount"": 7,
      ""ruleUpdatedAt"": ""2016-12-20T19:10:03+0100"",
      ""lastUsed"": ""2016-12-21T16:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-28T21:57:01+0200"",
      ""actions"": {
        ""edit"": true,
        ""setAsDefault"": false,
        ""copy"": false
      }
    }
  ],
  ""actions"": {
    ""create"": false
  }
}");

            var action = new Func<Task>(async () => await service.GetQualityProfileAsync("my_project", "my_organization", SonarQubeLanguage.CSharp,
                CancellationToken.None));

            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("The SonarC# plugin is not installed on the connected SonarQube.");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetQualityProfile_Old_NoQualityProfile_Eg_NoSonarCSharpInstalled()
        {
            await ConnectToSonarQube();

            // Only a java Quality Profile is returned
            SetupRequest("api/qualityprofiles/search?projectKey=my_project&organization=my_organization", @"{
  ""profiles"": [
    {
      ""key"": ""AU-TpxcA-iU5OvuD2FL1"",
      ""name"": ""My BU Profile"",
      ""language"": ""java"",
      ""languageName"": ""Java"",
      ""isInherited"": true,
      ""isBuiltIn"": false,
      ""parentKey"": ""iU5OvuD2FLz"",
      ""parentName"": ""My Company Profile"",
      ""activeRuleCount"": 15,
      ""activeDeprecatedRuleCount"": 5,
      ""isDefault"": false,
      ""projectCount"": 7,
      ""ruleUpdatedAt"": ""2016-12-20T19:10:03+0100"",
      ""lastUsed"": ""2016-12-21T16:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-28T21:57:01+0200"",
      ""actions"": {
        ""edit"": true,
        ""setAsDefault"": false,
        ""copy"": false
      }
    }
  ],
  ""actions"": {
    ""create"": false
  }
}");

            var action = new Func<Task>(async () => await service.GetQualityProfileAsync("my_project", "my_organization", SonarQubeLanguage.CSharp,
                CancellationToken.None));

            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("The SonarC# plugin is not installed on the connected SonarQube.");

            messageHandler.VerifyAll();
        }
    }
}
