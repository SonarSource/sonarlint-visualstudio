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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests;

[TestClass]
public class SonarQubeService_GetAllQualityProfilesAsync : SonarQubeService_TestBase
{
    [TestMethod]
    public async Task GetAllQualityProfilesAsync_Old_ExampleFromSonarQube()
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
      ""rulesUpdatedAt"": ""2016-12-22T19:10:03+0100"",
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
      ""rulesUpdatedAt"": ""2016-12-20T19:10:03+0100"",
      ""lastUsed"": ""2016-12-21T16:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-28T21:57:01+0200"",
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
      ""rulesUpdatedAt"": ""2014-12-22T19:10:03+0100"",
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
        
        var result = await service.GetAllQualityProfilesAsync("my_project", 
            "my_organization", 
            CancellationToken.None);

        messageHandler.VerifyAll();
        result.Select(x => x.Key).Should().BeEquivalentTo(
            new []{"AU-TpxcA-iU5OvuD2FL3", "AU-TpxcA-iU5OvuD2FL1", "AU-TpxcB-iU5OvuD2FL7"});
        result.Select(x => x.TimeStamp).Should().BeEquivalentTo(new[]
            { "2016-12-22T19:10:03+0100", "2016-12-20T19:10:03+0100", "2014-12-22T19:10:03+0100" }
            .Select(DateTime.Parse));
    }

    [TestMethod]
    public async Task GetAllQualityProfilesAsync_Old_ProjectNotAnalyzed()
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
      ""rulesUpdatedAt"": ""2016-12-22T19:10:03+0100"",
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

        var result = (await service.GetAllQualityProfilesAsync("my_project", 
                "my_organization", 
                CancellationToken.None))
            .Single();

        messageHandler.VerifyAll();

        result.Should().NotBeNull();
        result.IsDefault.Should().BeTrue();
        result.Key.Should().Be("AU-TpxcA-iU5OvuD2FL3");
        result.Language.Should().Be("cs");
        result.Name.Should().Be("Sonar way");
        result.TimeStamp.Should().Be(DateTime.Parse("2016-12-22T19:10:03+0100"));
    }

    [TestMethod]
    public async Task GetAllQualityProfilesAsync_Old_Error()
    {
        await ConnectToSonarQube();

        SetupRequest("api/qualityprofiles/search?projectKey=my_project&organization=my_organization", "",
            HttpStatusCode.InternalServerError);

        Func<Task<IList<SonarQubeQualityProfile>>> func = async () =>
            await service.GetAllQualityProfilesAsync("my_project", "my_organization", CancellationToken.None);

        func.Should().ThrowExactly<HttpRequestException>().And
            .Message.Should().Be("Response status code does not indicate success: 500 (Internal Server Error).");

        messageHandler.VerifyAll();
    }

    [TestMethod]
    public async Task GetAllQualityProfiles_New_ExampleFromSonarQube()
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
      ""rulesUpdatedAt"": ""2016-12-22T19:10:03+0100"",
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
      ""rulesUpdatedAt"": ""2016-12-20T19:10:03+0100"",
      ""lastUsed"": ""2016-12-21T16:10:03+0100"",
      ""userUpdatedAt"": ""2016-06-28T21:57:01+0200"",
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
      ""rulesUpdatedAt"": ""2014-12-22T19:10:03+0100"",
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

        var result = await service.GetAllQualityProfilesAsync("my_project", 
            "my_organization",
            CancellationToken.None);

        messageHandler.VerifyAll();
        result.Select(x => x.Key).Should().BeEquivalentTo(
            new []{"AU-TpxcA-iU5OvuD2FL3", "AU-TpxcA-iU5OvuD2FL1", "AU-TpxcB-iU5OvuD2FL7"});
        result.Select(x => x.TimeStamp).Should().BeEquivalentTo(
            new[] { "2016-12-22T19:10:03+0100", "2016-12-20T19:10:03+0100", "2014-12-22T19:10:03+0100" }
            .Select(DateTime.Parse));

        // Regression test for #1450 - shouldn't get a warning about max items retrieved
        // https://github.com/SonarSource/sonarlint-visualstudio/issues/1450
        logger.WarningMessages.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task GetAllQualityProfiles_New_ProjectNotAnalyzed()
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
      ""rulesUpdatedAt"": ""2016-12-22T19:10:03+0100"",
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
        
        var result = (await service.GetAllQualityProfilesAsync("my_project", 
                "my_organization", 
                CancellationToken.None))
            .Single();

        result.IsDefault.Should().BeTrue();
        result.Key.Should().Be("AU-TpxcA-iU5OvuD2FL3");
        result.Language.Should().Be("cs");
        result.Name.Should().Be("Sonar way");
        result.TimeStamp.Should().Be(DateTime.Parse("2016-12-22T19:10:03+0100"));
    }

    [TestMethod]
    public void GetAllQualityProfiles_NotConnected()
    {
        // No calls to Connect
        // No need to setup request, the operation should fail

        var func = new Func<Task>(async () => await service.GetAllQualityProfilesAsync("my_project", 
            "my_organization",
            CancellationToken.None));

        func.Should().ThrowExactly<InvalidOperationException>().And
            .Message.Should().Be("This operation expects the service to be connected.");

        logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
    }
}
