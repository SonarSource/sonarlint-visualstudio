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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Service;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BoundSonarQubeProjectExtensionsTests
    {
        [TestMethod]
        public void CreateConnectionInformation_ArgCheck()
        {
            Exceptions.Expect<ArgumentNullException>(() => BoundSonarQubeProjectExtensions.CreateConnectionInformation(null));
        }

        [TestMethod]
        public void CreateConnectionInformation_NoCredentials()
        {
            // Setup
            var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey");

            // Act
            ConnectionInformation conn = input.CreateConnectionInformation();

            // Verify
            Assert.AreEqual(input.ServerUri, conn.ServerUri);
            Assert.IsNull(conn.UserName);
            Assert.IsNull(conn.Password);
        }

        [TestMethod]
        public void CreateConnectionInformation_BasicAuthCredentials()
        {
            // Setup
            var creds = new BasicAuthCredentials("UserName", "password".ToSecureString());
            var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey", creds);

            // Act
            ConnectionInformation conn = input.CreateConnectionInformation();

            // Verify
            Assert.AreEqual(input.ServerUri, conn.ServerUri);
            Assert.AreEqual(creds.UserName, conn.UserName);
            Assert.AreEqual(creds.Password.ToUnsecureString(), conn.Password.ToUnsecureString());
        }
    }
}
