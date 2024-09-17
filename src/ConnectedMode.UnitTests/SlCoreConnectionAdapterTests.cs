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

using System.Security;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class SlCoreConnectionAdapterTests
{
    private readonly BasicAuthCredentials validToken = new ("I_AM_JUST_A_TOKEN", new SecureString());
    
    private SlCoreConnectionAdapter testSubject;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private IConnectionConfigurationSLCoreService connectionConfigurationSlCoreService;
    private ConnectionInfo sonarCloudConnectionInfo;
    private ConnectionInfo sonarQubeConnectionInfo;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        threadHandling = new NoOpThreadHandler();
        logger = Substitute.For<ILogger>();
        connectionConfigurationSlCoreService = Substitute.For<IConnectionConfigurationSLCoreService>();
        testSubject = new SlCoreConnectionAdapter(slCoreServiceProvider, threadHandling, logger);

        SetupConnection();
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_SwitchesToBackgroundThread()
    {
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var slCoreConnectionAdapter = new SlCoreConnectionAdapter(slCoreServiceProvider, threadHandlingMock, logger);

        await slCoreConnectionAdapter.ValidateConnectionAsync(sonarQubeConnectionInfo, new TokenCredentialsModel("myToken".CreateSecureString()));

        await threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<AdapterResponse>>>());
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_GettingConnectionConfigurationSLCoreServiceFails_ReturnsUnsuccessfulResponseAndLogs()
    {
        slCoreServiceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService _).Returns(false);

        var response = await testSubject.ValidateConnectionAsync(sonarQubeConnectionInfo, new TokenCredentialsModel("myToken".CreateSecureString()));

        logger.Received(1).LogVerbose($"[{nameof(IConnectionConfigurationSLCoreService)}] {SLCoreStrings.ServiceProviderNotInitialized}");
        response.Success.Should().BeFalse();
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarQubeWithToken_CallsValidateConnectionWithCorrectParams()
    {
        var token = "myToken";

        await testSubject.ValidateConnectionAsync(sonarQubeConnectionInfo, new TokenCredentialsModel(token.CreateSecureString()));

        await connectionConfigurationSlCoreService.Received(1)
            .ValidateConnectionAsync(Arg.Is<ValidateConnectionParams>(x => IsExpectedSonarQubeConnectionParams(x, token)));
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarQubeWithCredentials_CallsValidateConnectionWithCorrectParams()
    {
        var username = "username";
        var password = "password";

        await testSubject.ValidateConnectionAsync(sonarQubeConnectionInfo, new UsernamePasswordModel(username, password.CreateSecureString()));

        await connectionConfigurationSlCoreService.Received(1)
            .ValidateConnectionAsync(Arg.Is<ValidateConnectionParams>(x => IsExpectedSonarQubeConnectionParams(x, username, password)));
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarCloudWithToken_CallsValidateConnectionWithCorrectParams()
    {
        var token = "myToken";

        await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, new TokenCredentialsModel(token.CreateSecureString()));

        await connectionConfigurationSlCoreService.Received(1)
            .ValidateConnectionAsync(Arg.Is<ValidateConnectionParams>(x => IsExpectedSonarCloudConnectionParams(x, token)));
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarCloudWithCredentials_CallsValidateConnectionWithCorrectParams()
    {
        var username = "username";
        var password = "password";

        await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, new UsernamePasswordModel(username, password.CreateSecureString()));

        await connectionConfigurationSlCoreService.Received(1)
            .ValidateConnectionAsync(Arg.Is<ValidateConnectionParams>(x => IsExpectedSonarCloudConnectionParams(x, username, password)));
    }

    [TestMethod]
    [DataRow(true, "success")]
    [DataRow(false, "failure")]
    public async Task ValidateConnectionAsync_ReturnsResponseFromSlCore(bool success, string message)
    {
        var expectedResponse = new ValidateConnectionResponse(success, message);
        connectionConfigurationSlCoreService.ValidateConnectionAsync(Arg.Any<ValidateConnectionParams>()).Returns(expectedResponse);

        var response = await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, new TokenCredentialsModel("myToken".CreateSecureString()));

        response.Success.Should().Be(success);
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_SlCoreValidationThrowsException_ReturnsUnsuccessfulResponse()
    {
        var exceptionMessage = "validation failed";
        connectionConfigurationSlCoreService.When(x => x.ValidateConnectionAsync(Arg.Any<ValidateConnectionParams>()))
            .Do(x => throw new Exception(exceptionMessage));

        var response = await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, new TokenCredentialsModel("token".CreateSecureString()));

        logger.Received(1).LogVerbose($"{Resources.ValidateCredentials_Fails}: {exceptionMessage}");
        response.Success.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_SwitchesToBackgroundThread()
    {
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var slCoreConnectionAdapter = new SlCoreConnectionAdapter(slCoreServiceProvider, threadHandlingMock, logger);

        await slCoreConnectionAdapter.GetOrganizationsAsync(new TokenCredentialsModel("token".CreateSecureString()));

        await threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<AdapterResponseWithData<List<OrganizationDisplay>>>>>());
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_GettingConnectionConfigurationSLCoreServiceFails_ReturnsFailedResponseAndShouldLog()
    {
        slCoreServiceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService _).Returns(false);

        var response = await testSubject.GetOrganizationsAsync(new TokenCredentialsModel("token".CreateSecureString()));

        logger.Received(1).LogVerbose($"[{nameof(IConnectionConfigurationSLCoreService)}] {SLCoreStrings.ServiceProviderNotInitialized}");
        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_SlCoreThrowsException_ReturnsFailedResponseAndShouldLog()
    {
        var exceptionMessage = "validation failed";
        connectionConfigurationSlCoreService.When(x => x.ListUserOrganizationsAsync(Arg.Any<ListUserOrganizationsParams>()))
            .Do(x => throw new Exception(exceptionMessage));

        var response = await testSubject.GetOrganizationsAsync(new TokenCredentialsModel("token".CreateSecureString()));

        logger.Received(1).LogVerbose($"{Resources.ListUserOrganizations_Fails}: {exceptionMessage}");
        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_TokenIsProvided_CallsSlCoreListUserOrganizationsWithToken()
    {
        var token = "token";

        await testSubject.GetOrganizationsAsync(new TokenCredentialsModel(token.CreateSecureString()));

        await connectionConfigurationSlCoreService.Received(1).ListUserOrganizationsAsync(Arg.Is<ListUserOrganizationsParams>(x=> IsExpectedCredentials(x.credentials, token)));
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_UsernameAndPasswordIsProvided_CallsSlCoreListUserOrganizationsWithUsernameAndPassword()
    {
        var username = "username";
        var password = "password";

        await testSubject.GetOrganizationsAsync(new UsernamePasswordModel(username, password.CreateSecureString()));

        await connectionConfigurationSlCoreService.Received(1).ListUserOrganizationsAsync(Arg.Is<ListUserOrganizationsParams>(x => IsExpectedCredentials(x.credentials, username, password)));
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_CredentialsIsNull_ReturnsFailedResponseAndShouldLog()
    {
        var response = await testSubject.GetOrganizationsAsync(null);

        logger.Received(1).LogVerbose($"{Resources.ListUserOrganizations_Fails}: Unexpected {nameof(ICredentialsModel)} argument");
        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_NoOrganizationExists_ReturnsSuccessResponseAndEmptyOrganizations()
    {
        connectionConfigurationSlCoreService.ListUserOrganizationsAsync(Arg.Any<ListUserOrganizationsParams>())
            .Returns(new ListUserOrganizationsResponse([]));

        var response = await testSubject.GetOrganizationsAsync(new TokenCredentialsModel("token".CreateSecureString()));

        response.Success.Should().BeTrue();
        response.ResponseData.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOrganizationsAsync_OrganizationExists_ReturnsSuccessResponseAndMappedOrganizations()
    {
        List<OrganizationDto> serverOrganizations = [new OrganizationDto("key", "name", "desc"), new OrganizationDto("key2", "name2", "desc2")];
        connectionConfigurationSlCoreService.ListUserOrganizationsAsync(Arg.Any<ListUserOrganizationsParams>())
            .Returns(new ListUserOrganizationsResponse(serverOrganizations));

        var response = await testSubject.GetOrganizationsAsync(new TokenCredentialsModel("token".CreateSecureString()));

        response.Success.Should().BeTrue();
        response.ResponseData.Should().BeEquivalentTo([
            new OrganizationDisplay("key", "name"),
            new OrganizationDisplay("key2", "name2")
        ]);
    }
    
    [TestMethod]
    public async Task GetServerProjectByKeyAsync_SwitchesToBackgroundThread()
    {
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var slCoreConnectionAdapter = new SlCoreConnectionAdapter(slCoreServiceProvider, threadHandlingMock, logger);

        await slCoreConnectionAdapter.GetServerProjectByKeyAsync(validToken, sonarCloudConnectionInfo, "server-project-key");

        await threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<AdapterResponseWithData<ServerProject>>>>());
    }
    
    [TestMethod]
    public async Task GetServerProjectByKeyAsync_GettingConnectionConfigurationSLCoreServiceFails_ReturnsFailedResponseAndShouldLog()
    {
        slCoreServiceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService _).Returns(false);

        var response = await testSubject.GetServerProjectByKeyAsync(validToken, sonarCloudConnectionInfo, "server-project-key");

        logger.Received(1).LogVerbose($"[{nameof(IConnectionConfigurationSLCoreService)}] {SLCoreStrings.ServiceProviderNotInitialized}");
        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeNull();
    }
    
    [TestMethod]
    public async Task GetServerProjectByKeyAsync_SlCoreThrowsException_ReturnsFailedResponseAndShouldLog()
    {
        const string exceptionMessage = "SLCore error";
        connectionConfigurationSlCoreService.When(x => x.GetProjectNamesByKeyAsync(Arg.Any<GetProjectNamesByKeyParams>()))
            .Do(x => throw new Exception(exceptionMessage));

        var response = await testSubject.GetServerProjectByKeyAsync(validToken, sonarCloudConnectionInfo, "server-project-key");

        logger.Received(1).LogVerbose($"{Resources.GetServerProjectByKey_Fails}: {exceptionMessage}");
        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeNull();
    }
    
    [TestMethod]
    public async Task GetServerProjectByKeyAsync_ProjectNotFound_ReturnsSuccessResponseAndMappedOrganizations()
    {
        var slCoreResponse = new Dictionary<string, string>();
        connectionConfigurationSlCoreService.GetProjectNamesByKeyAsync(Arg.Any<GetProjectNamesByKeyParams>())
            .Returns(new GetProjectNamesByKeyResponse(slCoreResponse));
        
        var response = await testSubject.GetServerProjectByKeyAsync(validToken, sonarCloudConnectionInfo, "project-key");

        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeNull();
    }
    
    [TestMethod]
    public async Task GetServerProjectByKeyAsync_ProjectFound_ReturnsSuccessResponseAndMappedOrganizations()
    {
        var slCoreResponse = new Dictionary<string, string>
        {
            {"project-key", "project-name"}
        };
        connectionConfigurationSlCoreService.GetProjectNamesByKeyAsync(Arg.Any<GetProjectNamesByKeyParams>())
            .Returns(new GetProjectNamesByKeyResponse(slCoreResponse));
        var securePassword = new SecureString();
        foreach (var c in "SHHHHH") {
            securePassword.AppendChar(c);
        }
        
        var response = await testSubject.GetServerProjectByKeyAsync(new BasicAuthCredentials("USERNAME", securePassword), sonarQubeConnectionInfo, "project-key");

        response.Success.Should().BeTrue();
        response.ResponseData.Should().BeEquivalentTo(new ServerProject("project-key", "project-name"));
    }

    private bool IsExpectedSonarQubeConnectionParams(ValidateConnectionParams receivedParams, string token)
    {
        var transientSonarQubeDto = receivedParams.transientConnection.Left;
        return transientSonarQubeDto.serverUrl == sonarQubeConnectionInfo.Id && IsExpectedCredentials(transientSonarQubeDto.credentials, token);
    }

    private bool IsExpectedSonarQubeConnectionParams(ValidateConnectionParams receivedParams, string username, string password)
    {
        var transientSonarQubeDto = receivedParams.transientConnection.Left;
        return transientSonarQubeDto.serverUrl == sonarQubeConnectionInfo.Id && IsExpectedCredentials(transientSonarQubeDto.credentials, username, password);
    }

    private static bool IsExpectedCredentials(Either<TokenDto, UsernamePasswordDto> credentials, string token)
    {
        return credentials.Left.token == token;
    }

    private static bool IsExpectedCredentials(Either<TokenDto, UsernamePasswordDto> credentials, string username, string password)
    {
        return credentials.Right.username == username && credentials.Right.password == password;
    }

    private bool IsExpectedSonarCloudConnectionParams(ValidateConnectionParams receivedParams, string token)
    {
        var transientSonarCloudDto = receivedParams.transientConnection.Right;
        return transientSonarCloudDto.organization == sonarCloudConnectionInfo.Id && IsExpectedCredentials(transientSonarCloudDto.credentials, token);
    }

    private bool IsExpectedSonarCloudConnectionParams(ValidateConnectionParams receivedParams, string username, string password)
    {
        var transientSonarCloudDto = receivedParams.transientConnection.Right;
        return transientSonarCloudDto.organization == sonarCloudConnectionInfo.Id && IsExpectedCredentials(transientSonarCloudDto.credentials, username, password);
    }

    private void SetupConnection()
    {
        sonarCloudConnectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud);
        sonarQubeConnectionInfo = new ConnectionInfo("http://localhost:9000", ConnectionServerType.SonarQube);
        slCoreServiceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService _).Returns(x =>
        {
            x[0] = connectionConfigurationSlCoreService;
            return true;
        });
    }
}
