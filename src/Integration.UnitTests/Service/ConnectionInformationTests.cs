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

using SonarLint.VisualStudio.Integration.Service;

using Xunit;
using System;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConnectionInformationTests
    {
        [Fact]
        public void ConnectionInformation_WithLoginInformation()
        {
            // Arrange
            var userName = "admin";
            var passwordUnsecure = "admin";
            var password = passwordUnsecure.ToSecureString();
            var serverUri = new Uri("http://localhost/");
            var testSubject = new ConnectionInformation(serverUri, userName, password);

            // Act
            password.Dispose(); // Connection information should maintain it's own copy of the password

            // Assert
            passwordUnsecure.Should().Be( testSubject.Password.ToUnsecureString(), "Password doesn't match");
            userName.Should().Be( testSubject.UserName, "UserName doesn't match");
            serverUri.Should().Be( testSubject.ServerUri, "ServerUri doesn't match");

            // Act clone
            var testSubject2 = (ConnectionInformation)((ICloneable)testSubject).Clone();

            // Now dispose the test subject
            testSubject.Dispose();

            // Assert testSubject
            Action act = () => testSubject.Password.ToUnsecureString();
            act.ShouldThrow<ObjectDisposedException>();

            // Assert testSubject2
            passwordUnsecure.Should().Be( testSubject2.Password.ToUnsecureString(), "Password doesn't match");
            userName.Should().Be( testSubject2.UserName, "UserName doesn't match");
            serverUri.Should().Be( testSubject2.ServerUri, "ServerUri doesn't match");
        }

        [Fact]
        public void ConnectionInformation_WithoutLoginInformation()
        {
            // Arrange
            var serverUri = new Uri("http://localhost/");

            // Act
            var testSubject = new ConnectionInformation(serverUri);

            // Assert
            testSubject.Password.Should().BeNull( "Password wasn't provided");
            testSubject.UserName.Should().BeNull( "UserName wasn't provided");
            serverUri.Should().Be( testSubject.ServerUri, "ServerUri doesn't match");

            // Act clone
            var testSubject2 = (ConnectionInformation)((ICloneable)testSubject).Clone();

            // Assert testSubject2
            testSubject2.Password.Should().BeNull( "Password wasn't provided");
            testSubject2.UserName.Should().BeNull( "UserName wasn't provided");
            serverUri.Should().Be( testSubject2.ServerUri, "ServerUri doesn't match");
        }

        [Fact]
        public void ConnectionInformation_Ctor_NormalizesServerUri()
        {
            // Act
            var noSlashResult = new ConnectionInformation(new Uri("http://localhost/NoSlash"));

            // Assert
            noSlashResult.ServerUri.ToString().Should().Be("http://localhost/NoSlash/", "Unexpected normalization of URI without trailing slash");
        }

        [Fact]
        public void Ctor_WithNullServerUri_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new ConnectionInformation(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }
    }
}
