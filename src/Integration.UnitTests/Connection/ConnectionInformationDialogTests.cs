/*
 * SonarLint for Visual Studio
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
using System.Security;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectionInformationDialogTests
    {
        [TestMethod]
        public void ConnectionInformationDialog_CreateConnectionInformation_NullArgumentChecks()
        {
            // Arrange
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(null);

            // Test 1: null viewModel
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                ConnectionInformationDialog.CreateConnectionInformation(null, new SecureString());
            });

            // Test 2: null password
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                ConnectionInformationDialog.CreateConnectionInformation(viewModel, null);
            });
        }

        [TestMethod]
        public void ConnectionInformationDialog_CreateConnectionInformation_InvalidModel_ReturnsNull()
        {
            // Arrange
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(null);
            viewModel.IsValid.Should().BeFalse("Empty view model should be invalid");
            var emptyPassword = new SecureString();

            // Act
            ConnectionInformation connInfo;
            using (var assertIgnoreScope = new AssertIgnoreScope())
            {
                connInfo = ConnectionInformationDialog.CreateConnectionInformation(viewModel, emptyPassword);
            }

            // Assert
            connInfo.Should().BeNull("No ConnectionInformation should be returned with an invalid model");
        }

        [TestMethod]
        public void ConnectionInformationDialog_CreateConnectionInformation_ValidModel_ReturnsConnectionInformation()
        {
            // Arrange
            var serverUrl = "https://localhost";
            var username = "admin";
            var inputPlaintextPassword = "letmein";
            var securePassword = inputPlaintextPassword.ToSecureString();

            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(null);
            viewModel.ServerUrlRaw = serverUrl;
            viewModel.Username = username;
            viewModel.ValidateCredentials(securePassword);

            // Act
            ConnectionInformation connInfo = ConnectionInformationDialog.CreateConnectionInformation(viewModel, securePassword);

            // Assert
            connInfo.Should().NotBeNull("ConnectionInformation should be returned");
            connInfo.ServerUri.Should().Be(new Uri(serverUrl), "Server URI returned was different");
            connInfo.UserName.Should().Be(username, "Username returned was different");

            string outputPlaintextPassword = connInfo.Password.ToUnsecureString();
            outputPlaintextPassword.Should().Be(inputPlaintextPassword, "Password returned was different");
        }

        [TestMethod]
        public void ConnectionInformationDialog_CreateConnectionInformation_WithExistingConnection()
        {
            // Arrange
            var connectionInformation = new ConnectionInformation(new Uri("http://blablabla"), "admin", "P@ssword1".ToSecureString());

            // Act
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(connectionInformation);

            // Assert
            viewModel.ServerUrl.Should().Be(connectionInformation.ServerUri, "Unexpected ServerUrl");
            viewModel.Username.Should().Be(connectionInformation.UserName, "Unexpected UserName");
        }
    }
}