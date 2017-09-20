using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            var service = new SonarQubeService(client.Object);
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
            // Arrange
            var client = new Mock<ISonarQubeClient>();

            // Act
            var service = new SonarQubeService(client.Object);

            // Assert
            service.OrganizationsFeatureMinimalVersion.Should().Be(new Version(6, 2));
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
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "1.0.0.0" }));
            var service = new SonarQubeService(client.Object);
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
            var service = new SonarQubeService(client.Object);

            // Act
            Action action = () => service.EnsureIsConnected();

            // Assert
            action.ShouldThrow<InvalidOperationException>().WithMessage("This operation expects the service to be connected.");
        }

        [TestMethod]
        public async Task GetAllPluginsAsync_ReturnsExpectedResult()
        {
            // Arrange
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            client.Setup(x => x.GetPluginsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<PluginDTO>.Ok(new[] { new PluginDTO { Key = "key", Version = "version" } }));
            var service = new SonarQubeService(client.Object);
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
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            client.Setup(x => x.GetPropertiesAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<PropertyDTO>.Ok(new[] { new PropertyDTO { Key = "key", Value = "value" } }));
            var service = new SonarQubeService(client.Object);
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
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            var service = new SonarQubeService(client.Object);
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
            var roslynExport = new RoslynExportProfile();
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            client.Setup(x => x.GetRoslynExportProfileAsync(It.IsAny<ConnectionDTO>(), It.IsAny<RoslynExportProfileRequest>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<RoslynExportProfile>.Ok(roslynExport));
            var service = new SonarQubeService(client.Object);
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetRoslynExportProfileAsync("name", ServerLanguage.CSharp, CancellationToken.None);

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
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = version }));
            var service = new SonarQubeService(client.Object);
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
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            client.Setup(x => x.GetOrganizationsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<OrganizationRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Queue<Result<OrganizationDTO[]>>(
                        new Result<OrganizationDTO[]>[]
                        {
                            Result<OrganizationDTO[]>.Ok(new[] { new OrganizationDTO { Key = "key", Name = "name" } }),
                            Result<OrganizationDTO[]>.Ok(new OrganizationDTO[0])
                        }
                    ).Dequeue);
            var service = new SonarQubeService(client.Object);
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
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            client.Setup(x => x.GetProjectsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<ProjectDTO[]>.Ok(new[] { new ProjectDTO { Key = "key", Name = "name" } }));
            var service = new SonarQubeService(client.Object);
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
            var client = new Mock<ISonarQubeClient>();
            client.Setup(x => x.ValidateCredentialsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<CredentialsDTO>.Ok(new CredentialsDTO { AreValid = true }));
            client.Setup(x => x.GetVersionAsync(It.IsAny<ConnectionDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<VersionDTO>.Ok(new VersionDTO { Version = "5.6" }));
            client.Setup(x => x.GetComponentsSearchProjectsAsync(It.IsAny<ConnectionDTO>(), It.IsAny<ComponentRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Queue<Result<ComponentDTO[]>>(
                        new Result<ComponentDTO[]>[]
                        {
                            Result<ComponentDTO[]>.Ok(new[] { new ComponentDTO { Key = "key", Name = "name" } }),
                            Result<ComponentDTO[]>.Ok(new ComponentDTO[0])
                        }
                    ).Dequeue);
            var service = new SonarQubeService(client.Object);
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
    }
}
