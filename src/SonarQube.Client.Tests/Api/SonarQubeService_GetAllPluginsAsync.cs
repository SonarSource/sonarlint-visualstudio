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
    public class SonarQubeService_GetAllPluginsAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetPlugins_Old_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("api/updatecenter/installed_plugins",
                @"[
  {
    ""key"": ""findbugs"",
    ""name"": ""Findbugs"",
    ""version"": ""2.1""
  },
  {
    ""key"": ""l10nfr"",
    ""name"": ""French Pack"",
    ""version"": ""1.10""
  },
  {
    ""key"": ""jira"",
    ""name"": ""JIRA"",
    ""version"": ""1.2""
  }
]");

            var result = await service.GetAllPluginsAsync(CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);
            result.Select(x => x.Key).Should().BeEquivalentTo(new[] { "findbugs", "l10nfr", "jira" });
            result.Select(x => x.Version).Should().BeEquivalentTo(new[] { "2.1", "1.10", "1.2" });
        }

        [TestMethod]
        public async Task GetPlugins_V6_3_ExampleFromSonarQube()
        {
            await ConnectToSonarQube("6.3.0.0");

            SetupRequest("api/plugins/installed",
                @"{
  ""plugins"": [
    {
                ""key"": ""scmgit"",
      ""name"": ""Git"",
      ""description"": ""Git SCM Provider."",
      ""version"": ""1.0 (build 1234)"",
      ""license"": ""GNU LGPL 3"",
      ""organizationName"": ""SonarSource"",
      ""organizationUrl"": ""http://www.sonarsource.com"",
      ""editionBundled"": false,
      ""homepageUrl"": ""https://redirect.sonarsource.com/plugins/scmgit.html"",
      ""issueTrackerUrl"": ""http://jira.sonarsource.com/browse/SONARSCGIT"",
      ""implementationBuild"": ""9ce9d330c313c296fab051317cc5ad4b26319e07"",
      ""filename"": ""sonar-scm-git-plugin-1.0.jar"",
      ""hash"": ""abcdef123456"",
      ""sonarLintSupported"": false,
      ""updatedAt"": 123456789
    },
    {
                ""key"": ""java"",
      ""name"": ""Java"",
      ""description"": ""SonarQube rule engine."",
      ""version"": ""3.0"",
      ""license"": ""GNU LGPL 3"",
      ""organizationName"": ""SonarSource"",
      ""organizationUrl"": ""http://www.sonarsource.com"",
      ""editionBundled"": false,
      ""homepageUrl"": ""https://redirect.sonarsource.com/plugins/java.html"",
      ""issueTrackerUrl"": ""http://jira.sonarsource.com/browse/SONARJAVA"",
      ""implementationBuild"": ""65396a609ddface8b311a6a665aca92a7da694f1"",
      ""filename"": ""sonar-java-plugin-3.0.jar"",
      ""hash"": ""abcdef123456"",
      ""sonarLintSupported"": true,
      ""updatedAt"": 123456789
    },
    {
                ""key"": ""scmsvn"",
      ""name"": ""SVN"",
      ""description"": ""SVN SCM Provider."",
      ""version"": ""1.0 (Build 3456)"",
      ""license"": ""GNU LGPL 3"",
      ""organizationName"": ""SonarSource"",
      ""organizationUrl"": ""http://www.sonarsource.com"",
      ""editionBundled"": false,
      ""homepageUrl"": ""https://redirect.sonarsource.com/plugins/scmsvn.html"",
      ""issueTrackerUrl"": ""http://jira.sonarsource.com/browse/SONARSCSVN"",
      ""implementationBuild"": ""213fc8a8b582ff530b12dd4a59a6512be1071234"",
      ""filename"": ""sonar-scm-svn-plugin-1.0.jar"",
      ""hash"": ""abcdef123456"",
      ""sonarLintSupported"": false,
      ""updatedAt"": 123456789
    }
  ]
}");

            var result = await service.GetAllPluginsAsync(CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);
            result.Select(x => x.Key).Should().BeEquivalentTo(new[] { "scmgit", "java", "scmsvn" });
            result.Select(x => x.Version).Should().BeEquivalentTo(new[] { "1.0.0", "3.0.0", "1.0.0" });
        }

        [TestMethod]
        public async Task GetPlugins_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/updatecenter/installed_plugins", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubePlugin>>> func = async () =>
                await service.GetAllPluginsAsync(CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}
