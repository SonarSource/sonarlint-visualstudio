﻿/*
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

using System.IO;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.Credentials
{
    [TestClass]
    public class CredentialsViewModelTests
    {
        private CredentialsViewModel testSubject;
        private ConnectionInfo connectionInfo;

        [TestInitialize]
        public void TestInitialize()
        {
            connectionInfo = new ConnectionInfo("http://localhost:9000", ConnectionServerType.SonarQube);
            testSubject = new CredentialsViewModel(connectionInfo);
        }

        [TestMethod]
        public void SelectedAuthenticationType_ShouldBeTokenByDefault()
        {
            testSubject.SelectedAuthenticationType.Should().Be(UiResources.AuthenticationTypeOptionToken);
            testSubject.IsTokenAuthentication.Should().BeTrue();
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
            var viewModel = new CredentialsViewModel(new ConnectionInfo("http://sonarcloud.io/myorg", ConnectionServerType.SonarCloud));

            viewModel.AccountSecurityUrl.Should().Be(UiResources.SonarCloudAccountSecurityUrl);
        }

        [TestMethod]
        public void AccountSecurityUrl_ConnectionIsSonarQube_ReturnsSonarQubeUrl()
        {
            var qubeUrl = "http://localhost:9000/";
            var viewModel = new CredentialsViewModel(new ConnectionInfo(qubeUrl, ConnectionServerType.SonarQube));
            var expectedUrl = Path.Combine(qubeUrl, UiResources.SonarQubeAccountSecurityUrl);

            viewModel.AccountSecurityUrl.Should().Be(expectedUrl);
        }
    }
}
