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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConnectionInformationTests
    {
        [TestMethod]
        public void ConnectionInformation_WithLoginInformation()
        {
            // Setup
            var userName = "admin";
            var passwordUnsecure = "admin";
            var password = passwordUnsecure.ToSecureString();
            var serverUri = new Uri("http://localhost/");
            var testSubject = new ConnectionInformation(serverUri, userName, password);

            // Act
            password.Dispose(); // Connection information should maintain it's own copy of the password

            // Verify
            Assert.AreEqual(passwordUnsecure, testSubject.Password.ToUnsecureString(), "Password doesn't match");
            Assert.AreEqual(userName, testSubject.UserName, "UserName doesn't match");
            Assert.AreEqual(serverUri, testSubject.ServerUri, "ServerUri doesn't match");

            // Act clone
            var testSubject2 = (ConnectionInformation)((ICloneable)testSubject).Clone();

            // Now dispose the test subject
            testSubject.Dispose();

            // Verify testSubject
            Exceptions.Expect<ObjectDisposedException>(() => testSubject.Password.ToUnsecureString());

            // Verify testSubject2
            Assert.AreEqual(passwordUnsecure, testSubject2.Password.ToUnsecureString(), "Password doesn't match");
            Assert.AreEqual(userName, testSubject2.UserName, "UserName doesn't match");
            Assert.AreEqual(serverUri, testSubject2.ServerUri, "ServerUri doesn't match");
        }

        [TestMethod]
        public void ConnectionInformation_WithoutLoginInformation()
        {
            // Setup
            var serverUri = new Uri("http://localhost/");

            // Act
            var testSubject = new ConnectionInformation(serverUri);

            // Verify
            Assert.IsNull(testSubject.Password, "Password wasn't provided");
            Assert.IsNull(testSubject.UserName, "UserName wasn't provided");
            Assert.AreEqual(serverUri, testSubject.ServerUri, "ServerUri doesn't match");

            // Act clone
            var testSubject2 = (ConnectionInformation)((ICloneable)testSubject).Clone();

            // Verify testSubject2
            Assert.IsNull(testSubject2.Password, "Password wasn't provided");
            Assert.IsNull(testSubject2.UserName, "UserName wasn't provided");
            Assert.AreEqual(serverUri, testSubject2.ServerUri, "ServerUri doesn't match");
        }

        [TestMethod]
        public void ConnectionInformation_Ctor_NormalizesServerUri()
        {
            // Act
            var noSlashResult = new ConnectionInformation(new Uri("http://localhost/NoSlash"));

            // Verify
            Assert.AreEqual("http://localhost/NoSlash/", noSlashResult.ServerUri.ToString(), "Unexpected normalization of URI without trailing slash");
        }

        [TestMethod]
        public void ConnectionInformation_Ctor_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionInformation(null));
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionInformation(null, "user", "pwd".ToSecureString()));
        }
    }
}
