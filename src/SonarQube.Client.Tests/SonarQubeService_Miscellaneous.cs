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
using SonarQube.Client.Api;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_Miscellaneous
    {
        [TestMethod]
        public void SonarQubeService_Ctor_ArgumentChecks()
        {
            Action action;

            var logger = new TestLogger();

            action = () => new SonarQubeService(null, string.Empty, logger);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("messageHandler");

            action = () => new SonarQubeService(new Mock<HttpClientHandler>().Object, null, logger);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userAgent");

            action = () => new SonarQubeService(new Mock<HttpClientHandler>().Object, string.Empty, null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void SonarQubeService_GetOrganizationKeyForWebApiCalls()
        {
            // 1. Doesn't fail with missing keys
            CheckSameKeyReturnedAndNoLogOutput(null);
            CheckSameKeyReturnedAndNoLogOutput(string.Empty);

            // 2. No change for normal keys
            CheckSameKeyReturnedAndNoLogOutput("my.org.key");
            CheckSameKeyReturnedAndNoLogOutput("sonar.internal.testing.no.org.XXX");
            CheckSameKeyReturnedAndNoLogOutput("aaa.sonar.internal.testing.no.org");

            // 3. Special key is recognised
            CheckSpecialKeyIsRecognisedAndNullReturned("sonar.internal.testing.no.org");
            CheckSpecialKeyIsRecognisedAndNullReturned("SONAR.INTERNAL.TESTING.NO.ORG");
        }

        [TestMethod]
        [DataRow("http://localhost", false)]
        [DataRow("https://sonarcloud.io", true)]
        public async Task SonarQubeService_UsesFactorySelector(string serverUrl, bool isSonarCloud)
        {
            var logger = new TestLogger();
            var connectionInfo = new ConnectionInformation(new Uri(serverUrl));

            var requestFactoryMock = CreateRequestFactory("1.2.3");
            var selectorMock = new Mock<IRequestFactorySelector>();
            selectorMock.Setup(x => x.Select(isSonarCloud, logger)).Returns(requestFactoryMock.Object);

            var testSubject = new SonarQubeService(Mock.Of<HttpMessageHandler>(), "user-agent", logger, selectorMock.Object, Mock.Of<ISecondaryIssueHashUpdater>(), null);
            await testSubject.ConnectAsync(connectionInfo, CancellationToken.None);

            selectorMock.Verify(x => x.Select(isSonarCloud, logger), Times.Once);
            requestFactoryMock.Invocations.Count.Should().Be(2);

            testSubject.IsConnected.Should().BeTrue();

            var expectedServerType = isSonarCloud ? ServerType.SonarCloud : ServerType.SonarQube;
            testSubject.GetServerInfo().ServerType.Should().Be(expectedServerType);
            testSubject.GetServerInfo().Version.Should().Be(new Version(1, 2, 3));
        }

        [TestMethod]
        public async Task SonarQubeService_FactorySelector_RequestsNewFactoryOnEachConnect()
        {
            var logger = new TestLogger();
            var sonarQubeConnectionInfo = new ConnectionInformation(new Uri("http://sonarqube"));
            var sonarCloudConnectionInfo = new ConnectionInformation(new Uri("https://sonarcloud.io"));

            var qubeFactoryMock = CreateRequestFactory("1.2.3");
            var cloudFactoryMock = CreateRequestFactory("9.9");

            var selectorMock = new Mock<IRequestFactorySelector>();
            selectorMock.Setup(x => x.Select(false /* isSonarCloud */, logger)).Returns(qubeFactoryMock.Object);
            selectorMock.Setup(x => x.Select(true /* isSonarCloud */, logger)).Returns(cloudFactoryMock.Object);

            var testSubject = new SonarQubeService(Mock.Of<HttpMessageHandler>(), "user-agent", logger, selectorMock.Object, Mock.Of<ISecondaryIssueHashUpdater>(), null);

            // 1. Connect to SonarQube
            await testSubject.ConnectAsync(sonarQubeConnectionInfo, CancellationToken.None);

            qubeFactoryMock.Invocations.Count.Should().Be(2);
            cloudFactoryMock.Invocations.Count.Should().Be(0);
            testSubject.IsConnected.Should().BeTrue();
            testSubject.GetServerInfo().ServerType.Should().Be(ServerType.SonarQube);

            // 2. Disconnect
            testSubject.Disconnect();
            testSubject.IsConnected.Should().BeFalse();

            // 3. Connect to SonarCloud
            await testSubject.ConnectAsync(sonarCloudConnectionInfo, CancellationToken.None);

            qubeFactoryMock.Invocations.Count.Should().Be(2);
            cloudFactoryMock.Invocations.Count.Should().Be(2);
            testSubject.IsConnected.Should().BeTrue();
            testSubject.GetServerInfo().ServerType.Should().Be(ServerType.SonarCloud);
        }

        private static Mock<IRequestFactory> CreateRequestFactory(string versionResponse)
        {
            var requestFactoryMock = new Mock<IRequestFactory>();

            // ConnectAsync calls GetVersion and ValidateCredentials
            var getVersionMock = CreateGetVersionRequest(versionResponse);
            requestFactoryMock.Setup(x => x.Create<IGetVersionRequest>(It.IsAny<ServerInfo>())).Returns(getVersionMock.Object);

            var validateCredentialsMock = CreateValidateCredsRequest(true);
            requestFactoryMock.Setup(x => x.Create<IValidateCredentialsRequest>(It.IsAny<ServerInfo>())).Returns(validateCredentialsMock.Object);

            return requestFactoryMock;
        }

        private static Mock<IGetVersionRequest> CreateGetVersionRequest(string response)
        {
            var requestMock = new Mock<IGetVersionRequest>();
            requestMock.Setup(x => x.InvokeAsync(It.IsAny<HttpClient>(), CancellationToken.None)).Returns(Task.FromResult(response));
            return requestMock;
        }

        private static Mock<IValidateCredentialsRequest> CreateValidateCredsRequest(bool response)
        {
            var requestMock = new Mock<IValidateCredentialsRequest>();
            requestMock.Setup(x => x.InvokeAsync(It.IsAny<HttpClient>(), CancellationToken.None)).Returns(Task.FromResult(response));
            return requestMock;
        }

        private static void CheckSameKeyReturnedAndNoLogOutput(string key)
        {
            var testLogger = new TestLogger();
            SonarQubeService.GetOrganizationKeyForWebApiCalls(key, testLogger).Should().Be(key);
            testLogger.DebugMessages.Should().HaveCount(0);
        }

        private static void CheckSpecialKeyIsRecognisedAndNullReturned(string key)
        {
            var testLogger = new TestLogger();
            SonarQubeService.GetOrganizationKeyForWebApiCalls(key, testLogger).Should().BeNull();
            testLogger.DebugMessages.Should().HaveCount(1);
            testLogger.DebugMessages[0].Should().Contain("sonar.internal.testing.no.org");
        }
    }
}
