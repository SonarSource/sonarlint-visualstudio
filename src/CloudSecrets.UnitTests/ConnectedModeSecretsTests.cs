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

using SonarLint.VisualStudio.Core.Secrets;
using SonarQube.Client;

namespace SonarLint.VisualStudio.CloudSecrets.UnitTests
{
    [TestClass]
    public class ConnectedModeSecretsTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ConnectedModeSecrets, IConnectedModeSecrets>(
                MefTestHelpers.CreateExport<ISonarQubeService>());
        }

        [TestMethod]
        public void AreSecretsAvailable_ServiceIsNotConnected_NotAvailable()
        {
            var sonarQubeService = CreateSonarQubeServiceMock(false, null);

            var testSubject = new ConnectedModeSecrets(sonarQubeService.Object);

            var result = testSubject.AreSecretsAvailable();

            sonarQubeService.Verify(x => x.IsConnected, Times.Once);
            sonarQubeService.Verify(x => x.GetServerInfo(), Times.Never);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void AreSecretsAvailable_ServiceIsConnectedToSonarCloud_Available()
        {
            var serverInfo = CreateServerInfo(ServerType.SonarCloud);
            var sonarQubeService = CreateSonarQubeServiceMock(true, serverInfo);

            var testSubject = new ConnectedModeSecrets(sonarQubeService.Object);

            var result = testSubject.AreSecretsAvailable();

            sonarQubeService.Verify(x => x.IsConnected, Times.Once);
            sonarQubeService.Verify(x => x.GetServerInfo(), Times.Once);
            result.Should().BeTrue();
        }


        [TestMethod]
        [DataRow("9.9", true)]
        [DataRow("9.8", false)]
        public void AreSecretsAvailable_ServiceIsConnectedToSonarQube_CheckPluginVersion(string version, bool expectedResult)
        {
            var serverInfo = CreateServerInfo(ServerType.SonarQube, new Version(version));
            var sonarQubeService = CreateSonarQubeServiceMock(true, serverInfo);

            var testSubject = new ConnectedModeSecrets(sonarQubeService.Object);

            var result = testSubject.AreSecretsAvailable();

            sonarQubeService.Verify(x => x.IsConnected, Times.Once);
            sonarQubeService.Verify(x => x.GetServerInfo(), Times.Once);
            result.Should().Be(expectedResult);
        }

        private ServerInfo CreateServerInfo(ServerType serverType, Version version = null)
        {
            version ??= new Version();
            var serverInfo = new ServerInfo(version, serverType);

            return serverInfo;
        }

        private Mock<ISonarQubeService> CreateSonarQubeServiceMock(bool isConnected, ServerInfo serverInfo)
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(isConnected);
            sonarQubeService.Setup(x => x.GetServerInfo()).Returns(serverInfo);

            return sonarQubeService;
        }
    }
}
