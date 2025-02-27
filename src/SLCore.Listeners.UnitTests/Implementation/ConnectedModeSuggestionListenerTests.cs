/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Binding;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class ConnectedModeSuggestionListenerTests
{
    private INoBindingSuggestionNotification noBindingSuggestionNotification;
    private ConnectedModeSuggestionListener testSubject;
    private IConnectedModeUIManager connectedModeUIManager;
    private readonly SonarQubeConnectionParams sonarQubeConnectionParams = new(new Uri("http://localhost:9000"), "a-token", Guid.NewGuid().ToString());
    private ILogger logger;
    private IIDEWindowService ideWindowService;

    [TestInitialize]
    public void TestInitialize()
    {
        noBindingSuggestionNotification = Substitute.For<INoBindingSuggestionNotification>();
        connectedModeUIManager = Substitute.For<IConnectedModeUIManager>();
        ideWindowService = Substitute.For<IIDEWindowService>();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        testSubject = new ConnectedModeSuggestionListener(noBindingSuggestionNotification, connectedModeUIManager, logger, ideWindowService);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeSuggestionListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<INoBindingSuggestionNotification>(),
            MefTestHelpers.CreateExport<IConnectedModeUIManager>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IIDEWindowService>());

    [TestMethod]
    public void MefCtor_IsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ConnectedModeSuggestionListener>();

    [TestMethod]
    public void Ctor_LoggerContextIsSet() => logger.Received(1).ForContext("Connected Mode Suggestion");

    [TestMethod]
    public async Task AssistCreatingConnectionAsync_IdeWindowIsBroughtToFront()
    {
        MockTrustConnectionDialogSucceeds();

        await testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams { connectionParams = sonarQubeConnectionParams });

        Received.InOrder(() =>
        {
            ideWindowService.BringToFront();
            connectedModeUIManager.ShowTrustConnectionDialogAsync(Arg.Any<ServerConnection>(), Arg.Any<string>());
        });
    }

    [TestMethod]
    public async Task AssistCreatingConnectionAsync_SonarQube_ShowsDialogWithCorrectParameters()
    {
        MockTrustConnectionDialogSucceeds();

        await testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams { connectionParams = sonarQubeConnectionParams });

        await connectedModeUIManager.Received(1)
            .ShowTrustConnectionDialogAsync(Arg.Is<ServerConnection.SonarQube>(x => x.ServerUri == sonarQubeConnectionParams.serverUrl && x.Credentials == null),
                Arg.Is<string>(x => x == sonarQubeConnectionParams.tokenValue));
    }

    [TestMethod]
    [DataRow(SonarCloudRegion.US)]
    [DataRow(SonarCloudRegion.EU)]
    public async Task AssistCreatingConnectionAsync_SonarCloud_ShowsDialogWithCorrectParameters(SonarCloudRegion expectedRegion)
    {
        MockTrustConnectionDialogSucceeds();
        var sonarCloudParams = CreateSonarCloudParams(expectedRegion);

        await testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams { connectionParams = sonarCloudParams });

        await connectedModeUIManager.Received(1)
            .ShowTrustConnectionDialogAsync(
                Arg.Is<ServerConnection.SonarCloud>(x => x.OrganizationKey == sonarCloudParams.organizationKey && x.Region.ToSlCoreRegion() == expectedRegion && x.Credentials == null),
                Arg.Is<string>(x => x == sonarCloudParams.tokenValue));
    }

    [TestMethod]
    public void AssistCreatingConnectionAsync_AssistCreatingConnectionParamsIsNull_ThrowsExceptionAndLogs()
    {
        MockTrustConnectionDialogSucceeds();

        var act = async () => await testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams { connectionParams = null });

        act.Should().Throw<ArgumentNullException>();
        logger.Received(1).LogVerbose(SLCoreStrings.AssistConnectionInvalidServerConnection, nameof(AssistCreatingConnectionParams));
    }

    [TestMethod]
    public async Task AssistCreatingConnectionAsync_SonarQube_TrustConnectionDialogSucceeds_ReturnsNewConnectionIdAndLogs()
    {
        MockTrustConnectionDialogSucceeds();
        var expectedNewConnectionId = new ServerConnection.SonarQube(sonarQubeConnectionParams.serverUrl).Id;

        var response = await testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams() { connectionParams = sonarQubeConnectionParams });

        response.Should().Be(new AssistCreatingConnectionResponse(expectedNewConnectionId));
        logger.Received(1).LogVerbose(SLCoreStrings.AssistConnectionSucceeds, expectedNewConnectionId);
    }

    [TestMethod]
    [DataRow(SonarCloudRegion.US)]
    [DataRow(SonarCloudRegion.EU)]
    public async Task AssistCreatingConnectionAsync_SonarCloud_TrustConnectionDialogSucceeds_ReturnsNewConnectionIdAndLogs(SonarCloudRegion expectedRegion)
    {
        MockTrustConnectionDialogSucceeds();
        var sonarCloudParams = CreateSonarCloudParams(expectedRegion);
        var expectedNewConnectionId = new ServerConnection.SonarCloud(sonarCloudParams.organizationKey, region: expectedRegion.ToCloudServerRegion()).Id;

        var response = await testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams() { connectionParams = sonarCloudParams });

        response.Should().Be(new AssistCreatingConnectionResponse(expectedNewConnectionId));
        logger.Received(1).LogVerbose(SLCoreStrings.AssistConnectionSucceeds, expectedNewConnectionId);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(false)]
    public void AssistCreatingConnectionAsync_TrustConnectionDialogFails_ReturnsNull(bool? failedResult)
    {
        MockTrustConnectionDialogFails(failedResult);

        var act = async () => await testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams() { connectionParams = sonarQubeConnectionParams });

        act.Should().Throw<OperationCanceledException>(SLCoreStrings.AssistConnectionCancelled);
    }

    [TestMethod]
    public void AssistBindingAsync_NotImplemented()
    {
        Action act = () => testSubject.AssistBindingAsync(new AssistBindingParams("A_CONNECTION_ID", "A_PROJECT_KEY", "A_CONFIG_SCOPE_ID", false));

        act.Should().Throw<NotImplementedException>();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void NoBindingSuggestionFound_Notifies(bool isSonarCloud)
    {
        var projectKey = "a-project-key";

        testSubject.NoBindingSuggestionFound(new NoBindingSuggestionFoundParams(projectKey, isSonarCloud));

        noBindingSuggestionNotification.Received().Show(projectKey, isSonarCloud);
    }

    private void MockTrustConnectionDialogSucceeds() => connectedModeUIManager.ShowTrustConnectionDialogAsync(Arg.Any<ServerConnection>(), Arg.Any<string>()).Returns(true);

    private void MockTrustConnectionDialogFails(bool? failedResult) => connectedModeUIManager.ShowTrustConnectionDialogAsync(Arg.Any<ServerConnection>(), Arg.Any<string>()).Returns(failedResult);

    private static SonarCloudConnectionParams CreateSonarCloudParams(SonarCloudRegion expectedRegion) => new("myOrg", "a-token", Guid.NewGuid().ToString(), expectedRegion);
}
