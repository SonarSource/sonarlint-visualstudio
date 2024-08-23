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

using System.ComponentModel;
using System.IO;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.SLCore.Service.Connection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.Credentials
{
    [TestClass]
    public class CredentialsViewModelTests
    {
        private CredentialsViewModel testSubject;
        private ConnectionInfo sonarQubeConnectionInfo; 
        private ConnectionInfo sonarCloudConnectionInfo;
        private ISlCoreConnectionAdapter slCoreConnectionAdapter;
        private IProgressReporterViewModel progressReporterViewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            sonarQubeConnectionInfo = new ConnectionInfo("http://localhost:9000", ConnectionServerType.SonarQube);
            sonarCloudConnectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud);
            slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
            progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();

            testSubject = new CredentialsViewModel(sonarQubeConnectionInfo, slCoreConnectionAdapter, progressReporterViewModel);
        }

        [TestMethod]
        public void SelectedAuthenticationType_ShouldBeTokenByDefault()
        {
            testSubject.SelectedAuthenticationType.Should().Be(UiResources.AuthenticationTypeOptionToken);
            testSubject.IsTokenAuthentication.Should().BeTrue();
        }

        [TestMethod]
        public void SelectedAuthenticationType_ClearsWarning()
        {
            testSubject.ProgressReporterViewModel.Warning = "credentials warning";

            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;

            testSubject.ProgressReporterViewModel.Received(1).Warning = null;
        }

        [TestMethod]
        public void IsTokenAuthentication_TokenIsSelected_ReturnsTrue()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;

            testSubject.IsTokenAuthentication.Should().BeTrue();
        }

        [TestMethod] public void IsTokenAuthentication_CredentialsIsSelected_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;

            testSubject.IsTokenAuthentication.Should().BeFalse();
        }

        [TestMethod]
        public void IsCredentialsAuthentication_TokenIsSelected_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;

            testSubject.IsCredentialsAuthentication.Should().BeFalse();
        }

        [TestMethod]
        public void IsCredentialsAuthentication_CredentialsIsSelected_ReturnsTrue()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;

            testSubject.IsCredentialsAuthentication.Should().BeTrue();
        }

        [TestMethod]
        public void IsConfirmationEnabled_TokenIsSelectedAndTokenIsFilled_ReturnsTrue()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;

            testSubject.Token = "dummy token";

            testSubject.IsConfirmationEnabled.Should().BeTrue();
        }

        [TestMethod]
        public void IsConfirmationEnabled_CorrectTokenIsProvidedAndValidationIsInProgress_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;
            testSubject.Token = "dummy token";

            testSubject.ProgressReporterViewModel.IsOperationInProgress.Returns(true);

            testSubject.IsConfirmationEnabled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void IsConfirmationEnabled_TokenIsSelectedAndTokenIsNotFilled_ReturnsFalse(string token)
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;

            testSubject.Token = token;

            testSubject.IsConfirmationEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void IsConfirmationEnabled_CredentialsIsSelectedAndUsernameAndPasswordAreFilled_ReturnsTrue()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;

            testSubject.Username = "dummy username";
            testSubject.Password = "dummy password";

            testSubject.IsConfirmationEnabled.Should().BeTrue();
        }

        [TestMethod]
        public void IsConfirmationEnabled_CorrectCredentialsProvidedAndValidationIsInProgress_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;
            testSubject.Username = "dummy username";
            testSubject.Password = "dummy password";

            testSubject.ProgressReporterViewModel.IsOperationInProgress.Returns(true);

            testSubject.IsConfirmationEnabled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null, "pwd")]
        [DataRow("", "pwd")]
        [DataRow("  ", "pwd")]
        [DataRow("username", null)]
        [DataRow("username", "")]
        [DataRow("username", "  ")]
        public void IsConfirmationEnabled_CredentialsIsSelectedAndUsernameOrPasswordAreNotFilled_ReturnsFalse(string username, string password)
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;

            testSubject.Username = username;
            testSubject.Password = password;

            testSubject.IsConfirmationEnabled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void ShouldTokenBeFilled_TokenAuthenticationIsSelectedAndTokenIsEmpty_ReturnsTrue(string token)
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;
            testSubject.Token = token;

            testSubject.ShouldTokenBeFilled.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldTokenBeFilled_TokenAuthenticationIsSelectedAndTokenIsFilled_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;
            testSubject.Token = "dummy token";

            testSubject.ShouldTokenBeFilled.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldTokenBeFilled_CredentialsAuthenticationIsSelected_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;

            testSubject.ShouldTokenBeFilled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void ShouldUsernameBeFilled_CredentialsAuthenticationIsSelectedAndUsernameIsEmpty_ReturnsTrue(string username)
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;
            testSubject.Username = username;

            testSubject.ShouldUsernameBeFilled.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldUsernameBeFilled_CredentialsAuthenticationIsSelectedAndUsernameIsFilled_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;
            testSubject.Username = "dummy username";

            testSubject.ShouldUsernameBeFilled.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldUsernameBeFilled_TokenAuthenticationIsSelected_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;

            testSubject.ShouldUsernameBeFilled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void ShouldPasswordBeFilled_CredentialsAuthenticationIsSelectedAndPasswordIsEmpty_ReturnsTrue(string password)
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;
            testSubject.Password = password;

            testSubject.ShouldPasswordBeFilled.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldPasswordBeFilled_CredentialsAuthenticationIsSelectedAndPasswordIsFilled_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;
            testSubject.Password = "dummy password";

            testSubject.ShouldPasswordBeFilled.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldPasswordBeFilled_TokenAuthenticationIsSelected_ReturnsFalse()
        {
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;

            testSubject.ShouldPasswordBeFilled.Should().BeFalse();
        }

        [TestMethod]
        public void AccountSecurityUrl_ConnectionIsSonarCloud_ReturnsSonarCloudUrl()
        {
            var viewModel = new CredentialsViewModel(sonarCloudConnectionInfo, slCoreConnectionAdapter, progressReporterViewModel);

            viewModel.AccountSecurityUrl.Should().Be(UiResources.SonarCloudAccountSecurityUrl);
        }

        [TestMethod]
        public void AccountSecurityUrl_ConnectionIsSonarQube_ReturnsSonarQubeUrl()
        {
            var qubeUrl = "http://localhost:9000/";
            var viewModel = new CredentialsViewModel(new ConnectionInfo(qubeUrl, ConnectionServerType.SonarQube), slCoreConnectionAdapter, progressReporterViewModel);
            var expectedUrl = Path.Combine(qubeUrl, UiResources.SonarQubeAccountSecurityUrl);

            viewModel.AccountSecurityUrl.Should().Be(expectedUrl);
        }

        [TestMethod]
        public async Task AdapterValidateConnectionAsync_TokenIsProvided_ShouldValidateConnectionWithToken()
        {
            MockAdapterValidateConnectionAsync();
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;
            testSubject.Token = "dummyToken";

            await testSubject.AdapterValidateConnectionAsync();

            await slCoreConnectionAdapter.Received(1).ValidateConnectionAsync(testSubject.ConnectionInfo, testSubject.Token);
        }

        [TestMethod]
        public async Task AdapterValidateConnectionAsync_CredentialsAreProvided_ShouldValidateConnectionWithToken()
        {
            MockAdapterValidateConnectionAsync();
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;
            testSubject.Username = "username";
            testSubject.Password = "password";

            await testSubject.AdapterValidateConnectionAsync();

            await slCoreConnectionAdapter.Received(1).ValidateConnectionAsync(testSubject.ConnectionInfo, testSubject.Username, testSubject.Password);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ValidateConnectionAsync_ReturnsResponseFromSlCore(bool success)
        {
            progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<AdapterResponse>>()).Returns(new AdapterResponse(success));

            var response = await testSubject.ValidateConnectionAsync();

            response.Should().Be(success);
        }

        [TestMethod]
        public async Task ValidateConnectionAsync_ReturnsResponseFromSlCore()
        {
            await testSubject.ValidateConnectionAsync();

            await progressReporterViewModel.Received(1)
                .ExecuteTaskWithProgressAsync(Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.TaskToPerform == testSubject.AdapterValidateConnectionAsync &&
                    x.ProgressStatus == UiResources.ValidatingConnectionProgressText &&
                    x.WarningText == UiResources.ValidatingConnectionFailedText &&
                    x.AfterProgressUpdated == testSubject.AfterProgressStatusUpdated));
        }

        [TestMethod]
        public void UpdateProgressStatus_RaisesEvents()
        {
            var eventHandler = Substitute.For<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler;
            eventHandler.ReceivedCalls().Should().BeEmpty();

            testSubject.AfterProgressStatusUpdated();

            eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConfirmationEnabled)));
        }

        [TestMethod]
        public void GetCredentialsModel_SelectedAuthenticationTypeIsToken_ReturnsModelWithToken()
        {
            testSubject.Token = "token";
            testSubject.Username = "username";
            testSubject.Password = "password";
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;

            var credentialsModel = testSubject.GetCredentialsModel();

            credentialsModel.Should().BeOfType<TokenCredentialsModel>();
            ((TokenCredentialsModel)credentialsModel).Token.Should().Be(testSubject.Token);
        }

        [TestMethod]
        public void GetCredentialsModel_SelectedAuthenticationTypeIsCredentials_ReturnsModelWithUsernameAndPassword()
        {
            testSubject.Token = "token";
            testSubject.Username = "username";
            testSubject.Password = "password";
            testSubject.SelectedAuthenticationType = UiResources.AuthenticationTypeOptionCredentials;

            var credentialsModel = testSubject.GetCredentialsModel();

            credentialsModel.Should().BeOfType<UsernamePasswordModel>();
            ((UsernamePasswordModel)credentialsModel).Username.Should().Be(testSubject.Username);
            ((UsernamePasswordModel)credentialsModel).Password.Should().Be(testSubject.Password);
        }

        private void MockAdapterValidateConnectionAsync(bool success = true)
        {
            slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<string>())
                .Returns(new AdapterResponse(success));
            slCoreConnectionAdapter.ValidateConnectionAsync(Arg.Any<ConnectionInfo>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new AdapterResponse(success));
        }
    }
}
