/*
 * SonarQube Client
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;

namespace SonarQube.Client.Services.Tests
{
    [TestClass]
    public class SonarQubeServiceTests
    {
        [TestMethod]
        public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperation()
        {
            // Arrange
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(new HttpResponseMessage(), new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(new HttpResponseMessage(), new VersionResponse { Version = "5.6" }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            Func<Task> func = async () =>
                await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Assert
            client.VerifyAll();
            func.ShouldThrow<InvalidOperationException>().WithMessage("This operation expects the service not to be connected.");
        }

        [TestMethod]
        public void Ctor_DefaultValues()
        {
            // Act &  Assert
            SonarQubeService.OrganizationsFeatureMinimalVersion.Should().Be(new Version(6, 2));
        }

        [TestMethod]
        public void Ctor_WithNullClient_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeService(null);

            // Assert
            action.ShouldThrow<ArgumentNullException>();
        }

        [TestMethod]
        public async Task EnsureIsConnected_WhenConnected_ShouldDoNothing()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "1.0.0.0" }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            Action action = () => service.EnsureIsConnected();

            // Assert
            client.VerifyAll();
            action.ShouldNotThrow<InvalidOperationException>();
        }

        [TestMethod]
        public void EnsureIsConnected_WhenNotConnected_ShouldThrow()
        {
            // Arrange
            var client = new Mock<ISonarQubeClient>();
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);

            // Act
            Action action = () => service.EnsureIsConnected();

            // Assert
            action.ShouldThrow<InvalidOperationException>().WithMessage("This operation expects the service to be connected.");
        }

        [TestMethod]
        public async Task GetAllPluginsAsync_ReturnsExpectedResult()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            client.Setup(x => x.GetPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<PluginResponse[]>(successResponse,
                    new[] { new PluginResponse { Key = "key", Version = "version" } }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetAllPluginsAsync(CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Key.Should().Be("key");
            result[0].Version.Should().Be("version");
        }

        [TestMethod]
        public async Task GetAllPropertiesAsync_ReturnsExpectedResult()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            client.Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<PropertyResponse[]>(successResponse,
                    new[] { new PropertyResponse { Key = "key", Value = "value" } }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetAllPropertiesAsync(CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Key.Should().Be("key");
            result[0].Value.Should().Be("value");
        }

        [TestMethod]
        public async Task GetProjectDashboardUrl_ReturnsExpectedUrl()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = service.GetProjectDashboardUrl("myProject");

            // Assert
            client.VerifyAll();
            result.Host.Should().Be("mysq.com");
            result.LocalPath.Should().Be("/dashboard/index/myProject");
        }

        [TestMethod]
        public async Task GetRoslynExportProfileAsync_ReturnsExpectedResult()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var roslynExport = new RoslynExportProfileResponse();
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            client.Setup(x => x.GetRoslynExportProfileAsync(It.IsAny<RoslynExportProfileRequest>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<RoslynExportProfileResponse>(successResponse, roslynExport));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetRoslynExportProfileAsync("name", SonarQubeLanguage.CSharp, CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().Be(roslynExport);
        }

        [TestMethod]
        public async Task HasOrganizationsFeature_WhenConnectedToSQSInferiorTo62_ReturnsFalse()
        {
            await HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected("5.6", false);
            await HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected("6.0", false);
            await HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected("6.1", false);
        }

        [TestMethod]
        public async Task HasOrganizationsFeature_WhenConnectedToSQSuperiorTo62_ReturnsTrue()
        {
            await HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected("6.2", true);
            await HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected("6.3", true);
            await HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected("6.4", true);
            await HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected("6.5", true);
        }
        private async Task HasOrganizationsFeature_WhenConnectedToSQVersion_ReturnsExpected(string version, bool expected)
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = version }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = service.HasOrganizationsFeature;

            // Assert
            client.VerifyAll();
            result.Should().Be(expected);
        }

        [TestMethod]
        public async Task GetAllOrganizationsAsync_ReturnsExpectedResult()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            client.Setup(x => x.GetOrganizationsAsync(It.IsAny<OrganizationRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Queue<Result<OrganizationResponse[]>>(
                        new Result<OrganizationResponse[]>[]
                        {
                            new Result<OrganizationResponse[]>(successResponse,
                                new[] { new OrganizationResponse { Key = "key", Name = "name" } }),
                            new Result<OrganizationResponse[]>(successResponse, new OrganizationResponse[0])
                        }
                    ).Dequeue);
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetAllOrganizationsAsync(CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Key.Should().Be("key");
            result[0].Name.Should().Be("name");
        }

        [TestMethod]
        public async Task GetAllProjectsAsync_WhenNoOrganizationIsSpecified_ReturnsExpectedResult()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            client.Setup(x => x.GetProjectsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<ProjectResponse[]>(successResponse,
                    new[] { new ProjectResponse { Key = "key", Name = "name" } }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetAllProjectsAsync(null, CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Key.Should().Be("key");
            result[0].Name.Should().Be("name");
        }

        [TestMethod]
        public async Task GetAllProjectsAsync_WhenOrganizationIsSpecified_ReturnsExpectedResult()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            client.Setup(x => x.GetComponentsSearchProjectsAsync(It.IsAny<ComponentRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Queue<Result<ComponentResponse[]>>(
                        new Result<ComponentResponse[]>[]
                        {
                            new Result<ComponentResponse[]>(successResponse, new[] { new ComponentResponse { Key = "key", Name = "name" } }),
                            new Result<ComponentResponse[]>(successResponse, new ComponentResponse[0])
                        }
                    ).Dequeue);
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetAllProjectsAsync("org", CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Key.Should().Be("key");
            result[0].Name.Should().Be("name");
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_ReturnsExpectedResults()
        {
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<CredentialResponse>(successResponse, new CredentialResponse { IsValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<VersionResponse>(successResponse, new VersionResponse { Version = "5.6" }));
            client.Setup(x => x.GetIssuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Result<ServerIssue[]>(successResponse,
                    new[]
                    {
                        new ServerIssue { Resolution = "WONTFIX" },
                        new ServerIssue { Resolution = "FALSE-POSITIVE" },
                        new ServerIssue { Resolution = "FIXED" },
                        new ServerIssue { Resolution = "" },
                    }));
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory.Setup(x => x.Create(It.IsAny<ConnectionRequest>())).Returns(client.Object);
            var service = new SonarQubeService(clientFactory.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetSuppressedIssuesAsync("key", CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().HaveCount(3);
            result[0].ResolutionState.Should().Be(SonarQubeIssueResolutionState.WontFix);
            result[1].ResolutionState.Should().Be(SonarQubeIssueResolutionState.FalsePositive);
            result[2].ResolutionState.Should().Be(SonarQubeIssueResolutionState.Fixed);
        }
    }
}
