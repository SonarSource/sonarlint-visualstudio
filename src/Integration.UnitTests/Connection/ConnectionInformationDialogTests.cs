//-----------------------------------------------------------------------
// <copyright file="ConnectionInformationDialogTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarLint.VisualStudio.Integration.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectionInformationDialogTests
    {
        [TestMethod]
        public void ConnectionInformationDialog_CreateConnectionInformation_NullArgumentChecks()
        {
            // Setup
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
            // Setup
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(null);
            Assert.IsFalse(viewModel.IsValid, "Empty view model should be invalid");
            var emptyPassword = new SecureString();

            // Act
            ConnectionInformation connInfo;
            using (var assertIgnoreScope = new AssertIgnoreScope())
            {
                connInfo = ConnectionInformationDialog.CreateConnectionInformation(viewModel, emptyPassword);
            }

            // Verify
            Assert.IsNull(connInfo, "No ConnectionInformation should be returned with an invalid model");
        }

        [TestMethod]
        public void ConnectionInformationDialog_CreateConnectionInformation_ValidModel_ReturnsConnectionInformation()
        {
            // Setup
            var serverUrl = "https://localhost";
            var username = "admin";
            var inputPlaintextPassword = "letmein";
            var securePassword = inputPlaintextPassword.ConvertToSecureString();

            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(null);
            viewModel.ServerUrlRaw = serverUrl;
            viewModel.Username = username;
            viewModel.ValidateCredentials(securePassword);

            // Act
            ConnectionInformation connInfo = ConnectionInformationDialog.CreateConnectionInformation(viewModel, securePassword);

            // Verify
            Assert.IsNotNull(connInfo, "ConnectionInformation should be returned");
            Assert.AreEqual(new Uri(serverUrl), connInfo.ServerUri, "Server URI returned was different");
            Assert.AreEqual(username, connInfo.UserName, "Username returned was different");

            string outputPlaintextPassword = connInfo.Password.ConvertToUnsecureString();
            Assert.AreEqual(inputPlaintextPassword, outputPlaintextPassword, "Password returned was different");
        }

        [TestMethod]
        public void ConnectionInformationDialog_CreateConnectionInformation_WithExistingConnection()
        {
            // Setup
            var connectionInformation = new ConnectionInformation(new Uri("http://blablabla"), "admin", "P@ssword1".ConvertToSecureString());

            // Act
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(connectionInformation);

            // Verify
            Assert.AreEqual(connectionInformation.ServerUri, viewModel.ServerUrl, "Unexpected ServerUrl");
            Assert.AreEqual(connectionInformation.UserName, viewModel.Username, "Unexpected UserName");
        }

    }
}
