/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using FluentAssertions;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Security;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    public class ConnectionInformationDialogTests
    {
        [Fact]
        public void CreateConnectionInformation_WithNullViewModel_ThrowsArgumentNullException()
        {
            // Arrange
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(null);

            // Arrange + Act
            Action act = () => ConnectionInformationDialog.CreateConnectionInformation(null, new SecureString());

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("viewModel");
        }

        [Fact]
        public void CreateConnectionInformation_WithNullPassword_ThrowsArgumentNullException()
        {
            // Arrange
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(null);

            // Arrange + Act
            Action act = () => ConnectionInformationDialog.CreateConnectionInformation(viewModel, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("password");
        }

        [Fact]
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

        [Fact]
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
            username.Should().Be(connInfo.UserName, "Username returned was different");

            string outputPlaintextPassword = connInfo.Password.ToUnsecureString();
            inputPlaintextPassword.Should().Be(outputPlaintextPassword, "Password returned was different");
        }

        [Fact]
        public void ConnectionInformationDialog_CreateConnectionInformation_WithExistingConnection()
        {
            // Arrange
            var connectionInformation = new ConnectionInformation(new Uri("http://blablabla"), "admin", "P@ssword1".ToSecureString());

            // Act
            ConnectionInfoDialogViewModel viewModel = ConnectionInformationDialog.CreateViewModel(connectionInformation);

            // Assert
            connectionInformation.ServerUri.Should().Be(viewModel.ServerUrl, "Unexpected ServerUrl");
            connectionInformation.UserName.Should().Be(viewModel.Username, "Unexpected UserName");
        }

    }
}
