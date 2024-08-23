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

using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class SlCoreConnectionAdapterTests
{
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

        await slCoreConnectionAdapter.ValidateConnectionAsync(sonarQubeConnectionInfo, "myToken");

        await threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<ValidateConnectionResponse>>>());
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_GettingConnectionConfigurationSLCoreServiceFails_ReturnsUnsuccessfulResponseAndLogs()
    {
        slCoreServiceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService _).Returns(false);

        var response = await testSubject.ValidateConnectionAsync(sonarQubeConnectionInfo, "myToken");

        logger.Received(1).LogVerbose($"[{nameof(IConnectionConfigurationSLCoreService)}] {SLCoreStrings.ServiceProviderNotInitialized}");
        response.success.Should().BeFalse();
        response.message.Should().Be(UiResources.ValidatingConnectionFailedText);
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarQubeWithToken_CallsValidateConnectionWithCorrectParams()
    {
        var token = "myToken";

        await testSubject.ValidateConnectionAsync(sonarQubeConnectionInfo, token);

        await connectionConfigurationSlCoreService.Received(1)
            .ValidateConnectionAsync(Arg.Is<ValidateConnectionParams>(x => IsExpectedSonarQubeConnectionParams(x, token)));
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarQubeWithCredentials_CallsValidateConnectionWithCorrectParams()
    {
        var username = "username";
        var password = "password";

        await testSubject.ValidateConnectionAsync(sonarQubeConnectionInfo, username, password);

        await connectionConfigurationSlCoreService.Received(1)
            .ValidateConnectionAsync(Arg.Is<ValidateConnectionParams>(x => IsExpectedSonarQubeConnectionParams(x, username, password)));
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarCloudWithToken_CallsValidateConnectionWithCorrectParams()
    {
        var token = "myToken";

        await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, token);

        await connectionConfigurationSlCoreService.Received(1)
            .ValidateConnectionAsync(Arg.Is<ValidateConnectionParams>(x => IsExpectedSonarCloudConnectionParams(x, token)));
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ConnectionToSonarCloudWithCredentials_CallsValidateConnectionWithCorrectParams()
    {
        var username = "username";
        var password = "password";

        await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, username, password);

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

        var response = await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, "token");

        response.Should().BeEquivalentTo(expectedResponse);
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_SlCoreValidationThrowsException_ReturnsUnsuccessfulResponse()
    {
        var exceptionMessage = "validation failed";
        connectionConfigurationSlCoreService.When(x => x.ValidateConnectionAsync(Arg.Any<ValidateConnectionParams>()))
            .Do(x => throw new Exception(exceptionMessage));

        var response = await testSubject.ValidateConnectionAsync(sonarCloudConnectionInfo, "token");

        logger.Received(1).LogVerbose($"{Resources.ValidateCredentials_Fails}: {exceptionMessage}");
        response.success.Should().BeFalse();
        response.message.Should().Be(exceptionMessage);
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
