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

using Xunit;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Service;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class BoundSonarQubeProjectExtensionsTests
    {
        [Fact]
        public void CreateConnectionInformation_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => BoundSonarQubeProjectExtensions.CreateConnectionInformation(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateConnectionInformation_NoCredentials()
        {
            // Arrange
            var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey");

            // Act
            ConnectionInformation conn = input.CreateConnectionInformation();

            // Assert
            input.ServerUri.Should().Be(conn.ServerUri);
            conn.UserName.Should().BeNull();
            conn.Password.Should().BeNull();
        }

        [Fact]
        public void CreateConnectionInformation_BasicAuthCredentials()
        {
            // Arrange
            var creds = new BasicAuthCredentials("UserName", "password".ToSecureString());
            var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey", creds);

            // Act
            ConnectionInformation conn = input.CreateConnectionInformation();

            // Assert
            input.ServerUri.Should().Be(conn.ServerUri);
            creds.UserName.Should().Be(conn.UserName);
            creds.Password.ToUnsecureString().Should().Be(conn.Password.ToUnsecureString());
        }
    }
}
