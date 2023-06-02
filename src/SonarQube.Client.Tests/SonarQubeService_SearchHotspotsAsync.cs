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
using Moq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_SearchHotspotsAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetHotspotAsync_ReturnsExpected()
        {
            var projectKey = "ConsoleAppTest";
            var branch = "main";

            var request = $"api/hotspots/search?projectKey={projectKey}&branch={branch}&p=1&ps=500";

            var response = @"{
    ""paging"": {
        ""pageIndex"": 1,
        ""pageSize"": 500,
        ""total"": 3
    },
    ""hotspots"": [
        {
            ""key"": ""AYeAEDJo4iDoE9Luo7Y9"",
            ""component"": ""ConsoleAppTest:oldproject/Program.cs"",
            ""project"": ""ConsoleAppTest"",
            ""securityCategory"": ""insecure-conf"",
            ""vulnerabilityProbability"": ""LOW"",
            ""status"": ""TO_REVIEW"",
            ""line"": 9,
            ""message"": ""Make sure creating this cookie without setting the 'Secure' property is safe here."",
            ""author"": """",
            ""creationDate"": ""2023-04-14T13:58:47+0000"",
            ""updateDate"": ""2023-04-14T13:58:47+0000"",
            ""textRange"": {
                ""startLine"": 9,
                ""endLine"": 9,
                ""startOffset"": 34,
                ""endOffset"": 68
            },
            ""flows"": [],
            ""ruleKey"": ""csharpsquid:S2092"",
            ""messageFormattings"": []
        },
        {
            ""key"": ""AYgqCWBb8zNecLmhXpkj"",
            ""component"": ""ConsoleAppTest:WebApplication1/Controllers/HomeController.cs"",
            ""project"": ""ConsoleAppTest"",
            ""securityCategory"": ""others"",
            ""vulnerabilityProbability"": ""LOW"",
            ""status"": ""REVIEWED"",
            ""resolution"": ""FIXED"",
            ""line"": 77,
            ""message"": ""Make sure using this hardcoded IP address '192.168.12.42' is safe here."",
            ""author"": """",
            ""creationDate"": ""2023-05-17T14:06:47+0000"",
            ""updateDate"": ""2023-06-01T13:19:49+0000"",
            ""textRange"": {
                ""startLine"": 77,
                ""endLine"": 77,
                ""startOffset"": 21,
                ""endOffset"": 36
            },
            ""flows"": [],
            ""ruleKey"": ""csharpsquid:S1313"",
            ""messageFormattings"": []
        },
        {
            ""key"": ""AYeAEDJo4iDoE9Luo7Y8"",
            ""component"": ""ConsoleAppTest:oldproject/Program.cs"",
            ""project"": ""ConsoleAppTest"",
            ""securityCategory"": ""others"",
            ""vulnerabilityProbability"": ""LOW"",
            ""status"": ""TO_REVIEW"",
            ""line"": 9,
            ""message"": ""Make sure creating this cookie without the \""HttpOnly\"" flag is safe."",
            ""author"": """",
            ""creationDate"": ""2023-04-14T13:58:47+0000"",
            ""updateDate"": ""2023-04-14T13:58:47+0000"",
            ""textRange"": {
                ""startLine"": 9,
                ""endLine"": 9,
                ""startOffset"": 34,
                ""endOffset"": 68
            },
            ""flows"": [],
            ""ruleKey"": ""csharpsquid:S3330"",
            ""messageFormattings"": []
        }
    ],
    ""components"": [
        {
            ""key"": ""ConsoleAppTest"",
            ""qualifier"": ""TRK"",
            ""name"": ""ConsoleAppTest"",
            ""longName"": ""ConsoleAppTest""
        },
        {
            ""key"": ""ConsoleAppTest:WebApplication1/Controllers/HomeController.cs"",
            ""qualifier"": ""FIL"",
            ""name"": ""HomeController.cs"",
            ""longName"": ""WebApplication1/Controllers/HomeController.cs"",
            ""path"": ""WebApplication1/Controllers/HomeController.cs""
        },
        {
            ""key"": ""ConsoleAppTest:oldproject/Program.cs"",
            ""qualifier"": ""FIL"",
            ""name"": ""Program.cs"",
            ""longName"": ""oldproject/Program.cs"",
            ""path"": ""oldproject/Program.cs""
        }
    ]
}";

            await ConnectToSonarQube("9.7.0.0");

            SetupRequest(request, response);

            var result = await service.SearchHotspotsAsync(projectKey, branch, CancellationToken.None);

            messageHandler.VerifyAll();

            result.Count.Should().Be(3);

            result[0].HotspotKey.Should().Be("AYeAEDJo4iDoE9Luo7Y9");
            result[0].ComponentKey.Should().Be("ConsoleAppTest:oldproject/Program.cs");
            result[0].FilePath.Should().Be("oldproject\\Program.cs");
            result[0].Status.Should().Be("TO_REVIEW");
            result[0].Resolution.Should().BeNull();
            result[0].TextRange.StartLine.Should().Be(9);
            result[0].TextRange.EndLine.Should().Be(9);
            result[0].TextRange.StartOffset.Should().Be(34);
            result[0].TextRange.EndOffset.Should().Be(68);
            result[0].RuleKey.Should().Be("csharpsquid:S2092");

            result[1].HotspotKey.Should().Be("AYgqCWBb8zNecLmhXpkj");
            result[1].ComponentKey.Should().Be("ConsoleAppTest:WebApplication1/Controllers/HomeController.cs");
            result[1].FilePath.Should().Be("WebApplication1\\Controllers\\HomeController.cs");
            result[1].Status.Should().Be("REVIEWED");
            result[1].Resolution.Should().Be("FIXED");
            result[1].TextRange.StartLine.Should().Be(77);
            result[1].TextRange.EndLine.Should().Be(77);
            result[1].TextRange.StartOffset.Should().Be(21);
            result[1].TextRange.EndOffset.Should().Be(36);
            result[1].RuleKey.Should().Be("csharpsquid:S1313");

            result[2].HotspotKey.Should().Be("AYeAEDJo4iDoE9Luo7Y8");
            result[2].ComponentKey.Should().Be("ConsoleAppTest:oldproject/Program.cs");
            result[2].FilePath.Should().Be("oldproject\\Program.cs");
            result[2].Status.Should().Be("TO_REVIEW");
            result[2].Resolution.Should().BeNull();
            result[2].TextRange.StartLine.Should().Be(9);
            result[2].TextRange.EndLine.Should().Be(9);
            result[2].TextRange.StartOffset.Should().Be(34);
            result[2].TextRange.EndOffset.Should().Be(68);
            result[2].RuleKey.Should().Be("csharpsquid:S3330");
        }

        [TestMethod]
        public void GetHotspotAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IList<SonarQubeHotspotSearch>>> func = async () =>
                await service.SearchHotspotsAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }
    }
}
