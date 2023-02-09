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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Secrets;
using SonarLint.VisualStudio.Integration.UnitTests;
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
        public void AreSecretsAvailable_ProjectIsUnbound_NotAvailable()
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(false);

            var testSubject = new ConnectedModeSecrets(sonarQubeService.Object);

            var result = testSubject.AreSecretsAvailable();

            sonarQubeService.Verify(x => x.IsConnected, Times.Once);
            sonarQubeService.Verify(x => x.GetServerInfo(), Times.Never);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void AreSecretsAvailable_ProjectIsConnectedToSonarCloud_Available()
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(true);

            var serverInfo = CreateServerInfo(ServerType.SonarCloud);
            sonarQubeService.Setup(x => x.GetServerInfo()).Returns(serverInfo);

            var testSubject = new ConnectedModeSecrets(sonarQubeService.Object);

            var result = testSubject.AreSecretsAvailable();

            sonarQubeService.Verify(x => x.IsConnected, Times.Once);
            sonarQubeService.Verify(x => x.GetServerInfo(), Times.Once);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void AreSecretsAvailable_ProjectIsConnectedToSonarQube_BelowRequiredVersion_NotAvailable()
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(true);

            var serverInfo = CreateServerInfo(ServerType.SonarQube, new Version(1, 5));
            sonarQubeService.Setup(x => x.GetServerInfo()).Returns(serverInfo);

            var testSubject = new ConnectedModeSecrets(sonarQubeService.Object);

            var result = testSubject.AreSecretsAvailable();

            sonarQubeService.Verify(x => x.IsConnected, Times.Once);
            sonarQubeService.Verify(x => x.GetServerInfo(), Times.Once);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void AreSecretsAvailable_ProjectIsConnectedToSonarQub_AboveRequiredVersion_Available()
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(true);

            var serverInfo = CreateServerInfo(ServerType.SonarQube, new Version(9, 9));
            sonarQubeService.Setup(x => x.GetServerInfo()).Returns(serverInfo);

            var testSubject = new ConnectedModeSecrets(sonarQubeService.Object);

            var result = testSubject.AreSecretsAvailable();

            sonarQubeService.Verify(x => x.IsConnected, Times.Once);
            sonarQubeService.Verify(x => x.GetServerInfo(), Times.Once);
            result.Should().BeTrue();
        }

        private ServerInfo CreateServerInfo(ServerType serverType, Version version = null)
        {
            version ??= new Version();
            var serverInfo = new ServerInfo(version, serverType);

            return serverInfo;
        }
    }
}
