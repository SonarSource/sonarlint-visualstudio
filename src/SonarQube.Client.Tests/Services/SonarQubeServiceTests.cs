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
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Tests;

namespace SonarQube.Client.Services.Tests
{
    [TestClass]
    public class SonarQubeServiceTests
    {
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
            action.Should().ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperation()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            var service = new SonarQubeService(WrapInMockFactory(client));

            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            Func<Task> func = async () =>
                await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Assert
            client.VerifyAll();
            func.Should().ThrowExactly<InvalidOperationException>().WithMessage("This operation expects the service not to be connected.");
        }

        [TestMethod]
        public void ConnectAsync_WhenCredentialsAreInvalid_ThrowsExpectedException()
        {
            // Act
            var client = new Mock<ISonarQubeClient>();
            client
                .Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new CredentialResponse { IsValid = false }));

            var service = new SonarQubeService(WrapInMockFactory(client));
            Func<Task> func = async () =>
                await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Assert
            func.Should().ThrowExactly<Exception>().WithMessage("Invalid credentials.");
        }

        [TestMethod]
        public async Task EnsureIsConnected_WhenConnected_ShouldDoNothing()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("1.0.0.0");
            var service = new SonarQubeService(WrapInMockFactory(client));

            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            Action action = () => service.EnsureIsConnected();

            // Assert
            client.VerifyAll();
            action.Should().NotThrow<InvalidOperationException>();
        }

        [TestMethod]
        public void EnsureIsConnected_WhenNotConnected_Throws()
        {
            // Arrange
            var client = new Mock<ISonarQubeClient>();
            var service = new SonarQubeService(WrapInMockFactory(client));

            // Act
            Action action = () => service.EnsureIsConnected();

            // Assert
            action.Should().ThrowExactly<InvalidOperationException>().WithMessage("This operation expects the service to be connected.");
        }

        [TestMethod]
        public async Task GetAllPluginsAsync_ReturnsExpectedResult()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            client
                .Setup(x => x.GetPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[] { new PluginResponse { Key = "key", Version = "version" } }));

            var service = new SonarQubeService(WrapInMockFactory(client));

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
            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            client
                .Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[] { new PropertyResponse { Key = "key", Value = "value" } }));
            var service = new SonarQubeService(WrapInMockFactory(client));

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
            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            var service = new SonarQubeService(WrapInMockFactory(client));

            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = service.GetProjectDashboardUrl("myProject");

            // Assert
            client.VerifyAll();
            result.Host.Should().Be("mysq.com");
            result.LocalPath.Should().Be("/dashboard/index/myProject");
        }

        [TestMethod]
        public async Task GetRoslynExportProfileAsync_WhenServerIsLessThan66_ReturnsExpectedResult()
        {
            // Arrange
            var roslynExport = new RoslynExportProfileResponse();

            Expression<Func<RoslynExportProfileRequest, bool>> matchRequest = r =>
                r.GetType().Equals(typeof(RoslynExportProfileRequest)) && r.OrganizationKey == "my-org";

            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            client
                .Setup(x => x.GetRoslynExportProfileAsync(It.Is(matchRequest), CancellationToken.None))
                .ReturnsAsync(Result.Ok(roslynExport));

            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetRoslynExportProfileAsync("name", "my-org", SonarQubeLanguage.CSharp, CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().Be(roslynExport);
        }

        [TestMethod]
        public async Task GetRoslynExportProfileAsync_WhenServerIsGreaterThanOrEqualTo66_ReturnsExpectedResult()
        {
            // Arrange
            var roslynExport = new RoslynExportProfileResponse();

            Expression<Func<RoslynExportProfileRequestV66Plus, bool>> matchRequest = r =>
                r.OrganizationKey == "my-org";

            var client = GetMockSqClientWithCredentialAndVersion("6.6");
            client
                .Setup(x => x.GetRoslynExportProfileAsync(It.Is(matchRequest), CancellationToken.None))
                .ReturnsAsync(Result.Ok(roslynExport));

            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetRoslynExportProfileAsync("name", "my-org", SonarQubeLanguage.CSharp, CancellationToken.None);

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
            var client = GetMockSqClientWithCredentialAndVersion(version);
            var service = new SonarQubeService(WrapInMockFactory(client));

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
            var client = GetMockSqClientWithCredentialAndVersion("5.6");

            client
                .SetupSequence(x => x.GetOrganizationsAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[] { new OrganizationResponse { Key = "key", Name = "name" } }))
                .ReturnsAsync(Result.Ok(new OrganizationResponse[0]));

            var service = new SonarQubeService(WrapInMockFactory(client));
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
            var client = GetMockSqClientWithCredentialAndVersion("5.6");

            client
                .Setup(x => x.GetProjectsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[] { new ProjectResponse { Key = "key", Name = "name" } }));

            var service = new SonarQubeService(WrapInMockFactory(client));
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
            var client = GetMockSqClientWithCredentialAndVersion("5.6");

            client
                .SetupSequence(x => x.GetComponentsSearchProjectsAsync(
                    It.Is<ComponentRequest>(c => c.OrganizationKey == "org"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[] { new ComponentResponse { Key = "key", Name = "name" } }))
                .ReturnsAsync(Result.Ok(new ComponentResponse[0]));

            var service = new SonarQubeService(WrapInMockFactory(client));
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
            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            client
                .Setup(x => x.GetIssuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[]
                    {
                        new ServerIssue { Resolution = "WONTFIX" },
                        new ServerIssue { Resolution = "FALSE-POSITIVE" },
                        new ServerIssue { Resolution = "FIXED" },
                        new ServerIssue { Resolution = "" },
                    }));

            var service = new SonarQubeService(WrapInMockFactory(client));
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

        [TestMethod]
        public async Task GetNotificationEventsAsync_ReturnsExpectedResult()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("5.6");

            var expectedEvent = new NotificationsResponse
            {
                Category = "QUALITY_GATE",
                Link = new Uri("http://foo.com"),
                Date = new DateTimeOffset(2010, 1, 1, 14, 59, 59, TimeSpan.FromHours(2)),
                Message = "foo",
                Project = "test"
            };

            client
                .Setup(x => x.GetNotificationEventsAsync(It.IsAny<NotificationsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[] { expectedEvent }));

            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetNotificationEventsAsync("test", DateTimeOffset.Now, CancellationToken.None);

            // Assert
            client.VerifyAll();
            result.Should().NotBeNull();
            result.Should().HaveCount(1);

            result[0].Category.Should().Be(expectedEvent.Category);
            result[0].Link.Should().Be(expectedEvent.Link);
            result[0].Date.Should().Be(expectedEvent.Date);
            result[0].Message.Should().Be(expectedEvent.Message);
        }

        [TestMethod]
        public void ThrowWhenNotConnected()
        {
            // Arrange
            var client = new Mock<ISonarQubeClient>();
            var sqService = new SonarQubeService(WrapInMockFactory(client));

            // Act & Assert
            AssertExceptionThrownWhenNotConnected(() =>
                sqService.GetAllOrganizationsAsync(CancellationToken.None));

            AssertExceptionThrownWhenNotConnected(() =>
                sqService.GetAllPluginsAsync(CancellationToken.None));

            AssertExceptionThrownWhenNotConnected(() =>
                sqService.GetAllProjectsAsync("organizationKey", CancellationToken.None));

            AssertExceptionThrownWhenNotConnected(() =>
                sqService.GetAllPropertiesAsync(CancellationToken.None));

            AssertExceptionThrownWhenNotConnected(() =>
            {
                sqService.GetProjectDashboardUrl("projectKey");
                return Task.Delay(0);
            });

            AssertExceptionThrownWhenNotConnected(() =>
                sqService.GetQualityProfileAsync("projectKey", "some org", SonarQubeLanguage.CSharp, CancellationToken.None));

            AssertExceptionThrownWhenNotConnected(() =>
                sqService.GetRoslynExportProfileAsync("qualityProfileName", "some org", SonarQubeLanguage.CSharp, CancellationToken.None));

            AssertExceptionThrownWhenNotConnected(() =>
                sqService.GetNotificationEventsAsync("projectKey", DateTimeOffset.Now, CancellationToken.None));
        }

        private void AssertExceptionThrownWhenNotConnected(Func<Task> asyncCall)
        {
            asyncCall.Should().ThrowExactly<InvalidOperationException>()
                .And.Message.Should().Be("This operation expects the service to be connected.");
        }

        [TestMethod]
        public async Task Disconnect_WhenConnected_DisposeTheSonarQubeClient()
        {
            // Arrange
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            client.As<IDisposable>().Setup(x => x.Dispose()).Verifiable();
            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            service.Disconnect();

            // Assert
            client.VerifyAll();
        }

        [TestMethod]
        public void IsConnected_WhenNotConnected_ReturnsFalse()
        {
            // Arrange
            var service = new SonarQubeService(new SonarQubeClientFactory());

            // Act
            var result = service.IsConnected;

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task IsConnected_WhenConnected_ReturnsTrue()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("5.6");
            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = service.IsConnected;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task GetQualityProfileAsync_WhenOnlyOneProfile_ReturnsExpectedQualityProfile()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("0.0");
            client
                .Setup(x => x.GetQualityProfilesAsync(It.IsAny<QualityProfileRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[]
                    {
                        new QualityProfileResponse { Key = "QP_KEY", Language = "cs" }
                    }));

            client
                .Setup(x => x.GetQualityProfileChangeLogAsync(It.IsAny<QualityProfileChangeLogRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new QualityProfileChangeLogResponse
                    {
                        Events = new[] { new QualityProfileChangeLogEventResponse { Date = DateTime.MaxValue } }
                    }));

            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetQualityProfileAsync("PROJECT_KEY", "ORG_KEY", SonarQubeLanguage.CSharp, CancellationToken.None);

            // Assert
            client.Verify(x => x.GetQualityProfilesAsync(
                    It.Is<QualityProfileRequest>(r => r.Defaults == null && r.ProjectKey == "PROJECT_KEY" && r.OrganizationKey == "ORG_KEY"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            client.Verify(x => x.GetQualityProfileChangeLogAsync(It.IsAny<QualityProfileChangeLogRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
            result.Key.Should().Be("QP_KEY");
            result.Language.Should().Be("cs");
            result.TimeStamp.Should().Be(DateTime.MaxValue);
        }

        [TestMethod]
        public async Task GetQualityProfileAsync_WhenMultipleProfiles_ReturnsDefaultQualityProfile()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("0.0");
            client.Setup(x => x.GetQualityProfilesAsync(It.IsAny<QualityProfileRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new[]
                    {
                        new QualityProfileResponse { Key = "QP_KEY", Language = "cs" },
                        new QualityProfileResponse { Key = "QP_KEY_2", Language = "cs", IsDefault = true },
                    }));
            client.Setup(x => x.GetQualityProfileChangeLogAsync(It.IsAny<QualityProfileChangeLogRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new QualityProfileChangeLogResponse
                    {
                        Events = new[] { new QualityProfileChangeLogEventResponse { Date = DateTime.MaxValue } }
                    }));
            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetQualityProfileAsync("PROJECT_KEY", "ORG_KEY", SonarQubeLanguage.CSharp, CancellationToken.None);

            // Assert
            client.Verify(x => x.GetQualityProfilesAsync(
                    It.Is<QualityProfileRequest>(r => r.Defaults == null && r.ProjectKey == "PROJECT_KEY" && r.OrganizationKey == "ORG_KEY"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            client.Verify(x => x.GetQualityProfileChangeLogAsync(It.IsAny<QualityProfileChangeLogRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
            result.Key.Should().Be("QP_KEY_2");
            result.Language.Should().Be("cs");
            result.TimeStamp.Should().Be(DateTime.MaxValue);
        }

        [TestMethod]
        public async Task GetQualityProfileAsync_WhenError404_CallsAgainWithNoProjectKey()
        {
            // Arrange
            var client = GetMockSqClientWithCredentialAndVersion("0.0");
            client
                .SetupSequence(x => x.GetQualityProfilesAsync(It.IsAny<QualityProfileRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.NotFound(new QualityProfileResponse[0]))
                .ReturnsAsync(Result.Ok(new[] { new QualityProfileResponse { Key = "QP_KEY", Language = "cs" } }));

            client
                .Setup(x => x.GetQualityProfileChangeLogAsync(It.IsAny<QualityProfileChangeLogRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new QualityProfileChangeLogResponse
                    {
                        Events = new[] { new QualityProfileChangeLogEventResponse { Date = DateTime.MaxValue } }
                    }));

            var service = new SonarQubeService(WrapInMockFactory(client));
            await service.ConnectAsync(new ConnectionInformation(new Uri("http://mysq.com")), CancellationToken.None);

            // Act
            var result = await service.GetQualityProfileAsync("PROJECT_KEY", "ORG_KEY", SonarQubeLanguage.CSharp, CancellationToken.None);

            // Assert
            client.Verify(x => x.GetQualityProfilesAsync(
                    // Defaults is set to true in the client
                    It.Is<QualityProfileRequest>(r => r.Defaults == null && r.ProjectKey == null && r.OrganizationKey == "ORG_KEY"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            client.Verify(x => x.GetQualityProfilesAsync(
                    It.Is<QualityProfileRequest>(r => r.Defaults == null && r.ProjectKey == "PROJECT_KEY" && r.OrganizationKey == "ORG_KEY"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            client.Verify(x => x.GetQualityProfileChangeLogAsync(It.IsAny<QualityProfileChangeLogRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
            result.Key.Should().Be("QP_KEY");
            result.Language.Should().Be("cs");
            result.TimeStamp.Should().Be(DateTime.MaxValue);
        }

        private static Mock<ISonarQubeClient> GetMockSqClientWithCredentialAndVersion(string version)
        {
            var client = new Mock<ISonarQubeClient>();
            client
                .Setup(x => x.ValidateCredentialsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new CredentialResponse { IsValid = true }));
            client
                .Setup(x => x.GetVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok(new VersionResponse { Version = version }));

            return client;
        }

        private ISonarQubeClientFactory WrapInMockFactory(Mock<ISonarQubeClient> mockClient)
        {
            var clientFactory = new Mock<ISonarQubeClientFactory>();
            clientFactory
                .Setup(x => x.Create(It.IsAny<ConnectionRequest>()))
                .Returns(mockClient.Object);
            return clientFactory.Object;
        }
    }
}
