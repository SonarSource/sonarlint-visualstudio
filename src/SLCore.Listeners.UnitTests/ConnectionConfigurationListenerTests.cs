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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Connection;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests
{
    [TestClass]
    public class ConnectionConfigurationListenerTests
    {
        private IUpdateTokenNotification updateTokenNotification;
        private ConnectionConfigurationListener testSubject;
        private IServerConnectionWithInvalidTokenRepository serverConnectionWithInvalidTokenRepository;
        private IConfigurationProvider configurationProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            updateTokenNotification = Substitute.For<IUpdateTokenNotification>();
            serverConnectionWithInvalidTokenRepository = Substitute.For<IServerConnectionWithInvalidTokenRepository>();
            configurationProvider = Substitute.For<IConfigurationProvider>();
            testSubject = new ConnectionConfigurationListener(updateTokenNotification, serverConnectionWithInvalidTokenRepository, configurationProvider);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<ConnectionConfigurationListener, ISLCoreListener>(
                MefTestHelpers.CreateExport<IUpdateTokenNotification>(),
                MefTestHelpers.CreateExport<IServerConnectionWithInvalidTokenRepository>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>());

        [TestMethod]
        public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ConnectionConfigurationListener>();

        [TestMethod]
        [DataRow(null)]
        [DataRow(5)]
        [DataRow("something")]
        public void DidSynchronizeConfigurationScopesAsync_ReturnsTaskCompleted(object parameter)
        {
            var result = testSubject.DidSynchronizeConfigurationScopesAsync(parameter);

            result.Should().Be(Task.CompletedTask);
        }

        [TestMethod]
        public void InvalidToken_ConnectedMode_ConnectionIdIsCurrentConnection_CallsUpdateTokenNotificationAndUpdatesCache()
        {
            var sonarCloud = new ServerConnection.SonarCloud("myOrg");
            var parameters = new InvalidTokenParams(sonarCloud.Id);
            MockActiveSolutionBoundTrackerForConnectedMode(sonarCloud);

            testSubject.InvalidToken(parameters);

            updateTokenNotification.Received(1).Show(parameters.connectionId);
            serverConnectionWithInvalidTokenRepository.Received(1).AddConnectionIdWithInvalidToken(parameters.connectionId);
        }

        [TestMethod]
        public void InvalidToken_ConnectedMode_ConnectionIdForDifferentConnection_DoesNotCallUpdateTokenNotificationButUpdatesCache()
        {
            var parameters = new InvalidTokenParams("myConnectionId");
            MockActiveSolutionBoundTrackerForConnectedMode(new ServerConnection.SonarCloud("myOrg"));

            testSubject.InvalidToken(parameters);

            updateTokenNotification.DidNotReceiveWithAnyArgs().Show(Arg.Any<string>());
            serverConnectionWithInvalidTokenRepository.Received(1).AddConnectionIdWithInvalidToken(parameters.connectionId);
        }

        [TestMethod]
        public void InvalidToken_Standalone_CallsUpdateTokenNotificationAndUpdatesCache()
        {
            var parameters = new InvalidTokenParams("myConnectionId");
            MockActiveSolutionBoundTrackerForStandalone();

            testSubject.InvalidToken(parameters);

            updateTokenNotification.Received(1).Show(parameters.connectionId);
            serverConnectionWithInvalidTokenRepository.Received(1).AddConnectionIdWithInvalidToken(parameters.connectionId);
        }

        private void MockActiveSolutionBoundTrackerForStandalone() => configurationProvider.GetConfiguration().Returns(new BindingConfiguration(null, SonarLintMode.Standalone, null));

        private void MockActiveSolutionBoundTrackerForConnectedMode(ServerConnection boundServerConnection) =>
            configurationProvider.GetConfiguration().Returns(new BindingConfiguration(new BoundServerProject("solution", "projectKey", boundServerConnection),
                SonarLintMode.Connected, string.Empty));
    }
}
