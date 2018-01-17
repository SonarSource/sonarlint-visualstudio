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

namespace SonarQube.Client.Tests.Services
{
    [TestClass]
    public class SonarQubeService_GetAllPluginsAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetPlugins_ExampleFromSonarQube()
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
        public async Task GetPlugins_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("api/updatecenter/installed_plugins", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubePlugin>>> func = async () =>
                await service.GetAllPluginsAsync(CancellationToken.None);

            func.ShouldThrow<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }
    }
}
