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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Tests.Api
{
    [TestClass]
    public class SonarQubeService_GetAllModulesAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetModules_SonarQubeProjectWithTwoCSharpProjects()
        {
            await ConnectToSonarQube();

            SetupRequest("api/components/tree?qualifiers=BRC&component=myProject&p=1&ps=500", @"
{
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 2
  },
  ""baseComponent"": {
    ""key"": ""sq-project-key"",
    ""name"": ""SonarQube Project Name"",
    ""qualifier"": ""TRK""
  },
  ""components"": [
    {
      ""key"": ""sq-project-key:sq-project-key:50934C7A-2751-4675-91C4-CFD37C8BF57C"",
      ""name"": ""Project1"",
      ""qualifier"": ""BRC"",
      ""path"": ""src/Project1""
    },
    {
      ""key"": ""sq-project-key:sq-project-key:39CCD086-A7F8-42A0-B402-3C9BD9EB4825"",
      ""name"": ""Project2"",
      ""qualifier"": ""BRC"",
      ""path"": ""src/Project2""
    }
  ]
}");

            var result = await service.GetAllModulesAsync("myProject", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(3);

            result[0].Key.Should().Be("sq-project-key");
            result[0].Name.Should().Be("SonarQube Project Name");
            result[0].RelativePathToRoot.Should().BeNull();

            result[1].Key.Should().Be("sq-project-key:sq-project-key:50934C7A-2751-4675-91C4-CFD37C8BF57C");
            result[1].Name.Should().Be("Project1");
            result[1].RelativePathToRoot.Should().Be("src/Project1");

            result[2].Key.Should().Be("sq-project-key:sq-project-key:39CCD086-A7F8-42A0-B402-3C9BD9EB4825");
            result[2].Name.Should().Be("Project2");
            result[2].RelativePathToRoot.Should().Be("src/Project2");
        }

        [TestMethod]
        public async Task GetModules_SonarQubeProjectWithOneCSharpProject()
        {
            await ConnectToSonarQube();

            SetupRequest("api/components/tree?qualifiers=BRC&component=myProject&p=1&ps=500", @"
{
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 1
  },
  ""baseComponent"": {
    ""key"": ""sq-project-key"",
    ""name"": ""SonarQube Project Name"",
    ""qualifier"": ""TRK""
  },
  ""components"": [
    {
      ""key"": ""sq-project-key:sq-project-key:50934C7A-2751-4675-91C4-CFD37C8BF57C"",
      ""name"": ""Project1"",
      ""qualifier"": ""BRC""
    }
  ]
}");

            var result = await service.GetAllModulesAsync("myProject", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(2);

            result[0].Key.Should().Be("sq-project-key");
            result[0].Name.Should().Be("SonarQube Project Name");
            result[0].RelativePathToRoot.Should().BeNull();

            result[1].Key.Should().Be("sq-project-key:sq-project-key:50934C7A-2751-4675-91C4-CFD37C8BF57C");
            result[1].Name.Should().Be("Project1");
            result[1].RelativePathToRoot.Should().BeNull();            
        }

        [TestMethod]
        public async Task GetModules_ExpectedNewSonarQubeApi()
        {
            await ConnectToSonarQube();

            SetupRequest("api/components/tree?qualifiers=BRC&component=myProject&p=1&ps=500", @"
{
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 0
  },
  ""baseComponent"": {
    ""key"": ""sq-project-key"",
    ""name"": ""SonarQube Project Name"",
    ""qualifier"": ""TRK""
  },
  ""components"": []
}");

            var result = await service.GetAllModulesAsync("myProject", CancellationToken.None);

            messageHandler.VerifyAll();

            result.Should().HaveCount(1);

            result[0].Key.Should().Be("sq-project-key");
            result[0].Name.Should().Be("SonarQube Project Name");
            result[0].RelativePathToRoot.Should().BeNull();
        }
    }
}
