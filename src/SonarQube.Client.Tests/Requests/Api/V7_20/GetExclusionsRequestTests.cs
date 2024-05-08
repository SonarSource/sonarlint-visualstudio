/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Net.Http;
using SonarQube.Client.Api.V7_20;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests.Requests.Api.V7_20
{
    [TestClass]
    public class GetExclusionsRequestTests
    {
        [TestMethod]
        [DataRow("")]
        [DataRow(@"{}")]
        [DataRow(@"{""settings"": []}")]
        [DataRow(@"{""settings"": [{""key"": ""some.other.setting"",""values"": [""val1""]}]}")]
        public async Task InvokeAsync_AllSettingAreMissing_ReturnsEmptyConfiguration(string response)
        {
            const string projectKey = "myproject";

            var testSubject = CreateTestSubject(projectKey);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            var request = $"api/settings/values?component={projectKey}&keys=sonar.exclusions%2Csonar.global.exclusions%2Csonar.inclusions";

            SetupHttpRequest(handlerMock, request, response);

            var result = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            result.Should().NotBeNull();

            result.Exclusions.Should().BeEmpty();
            result.GlobalExclusions.Should().BeEmpty();
            result.Inclusions.Should().BeEmpty();
        }

        [TestMethod]
        [Description("This is what SonarCloud returns when the setting is not defined")]
        public async Task InvokeAsync_SomeMissingSetting_ReturnsDefinedProperties()
        {
            const string projectKey = "myproject";

            var testSubject = CreateTestSubject(projectKey);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            var request = $"api/settings/values?component={projectKey}&keys=sonar.exclusions%2Csonar.global.exclusions%2Csonar.inclusions";
            var response = @"{
	""settings"": [
		{
			""key"": ""sonar.global.exclusions"",
			""values"": [
				""**/build-wrapper-dump.json""
			]
		}
	]
}";

            SetupHttpRequest(handlerMock, request, response);

            var result = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            result.Should().NotBeNull();

            result.Exclusions.Should().BeEmpty();
            result.GlobalExclusions.Should().BeEquivalentTo("**/build-wrapper-dump.json");
            result.Inclusions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task InvokeAsync_ExistingSetting_ReturnsDefinedProperties()
        {
            const string projectKey = "myproject";

            var testSubject = CreateTestSubject(projectKey);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(ValidBaseAddress)
            };

            var request = $"api/settings/values?component={projectKey}&keys=sonar.exclusions%2Csonar.global.exclusions%2Csonar.inclusions";
            var response = @"{
	""settings"": [
		{
			""key"": ""sonar.exclusions"",
			""values"": [
				""**/value1"",
				""value2"",
				""some/value/3"",
			]
		},
		{
			""key"": ""sonar.global.exclusions"",
			""values"": [
				""some/value/4"",
			]
		},
		{
			""key"": ""sonar.inclusions"",
			""values"": [
                ""**/111""
            ]
		}
	]
}";

            SetupHttpRequest(handlerMock, request, response);

            var result = await testSubject.InvokeAsync(httpClient, CancellationToken.None);
            result.Should().NotBeNull();

            result.Exclusions.Should().BeEquivalentTo("**/value1", "**/value2", "**/some/value/3");
            result.GlobalExclusions.Should().BeEquivalentTo("**/some/value/4");
            result.Inclusions.Should().BeEquivalentTo("**/111");
        }
        

        private static GetExclusionsRequest CreateTestSubject(string projectKey)
        {
            var testSubject = new GetExclusionsRequest
            {
                Logger = new TestLogger(),
                ProjectKey = projectKey
            };

            return testSubject;
        }
    }
}
