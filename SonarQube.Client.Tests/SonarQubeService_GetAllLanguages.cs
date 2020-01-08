/*
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetAllLanguages : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetAllLanguages_Response_From_SonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("api/languages/list", @"
{
  ""languages"": [
    {
      ""key"": ""cs"",
      ""name"": ""C#"",
    },
    {
      ""key"": ""vbnet"",
      ""name"": ""VB.NET""
    }
  ]
}");

            var result = await service.GetAllLanguagesAsync(CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(2);

            result.Select(l => l.Key).Should().Contain(new[] { "cs", "vbnet" });
            result.Select(l => l.Name).Should().Contain(new[] { "C#", "VB.NET" });
            result.Select(l => l.PluginName).Should().Contain(new[] { "", "" });
        }

        [TestMethod]
        public void GetAllLanguages_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IList<SonarQubeLanguage>>> func = async () =>
                await service.GetAllLanguagesAsync(CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}
