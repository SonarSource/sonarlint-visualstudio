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

using System.Security;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Models
{
    [TestClass]
    public class ConnectionInformationTests
    {
        [TestMethod]
        public void Ctor_InvalidServerUrl_Throws()
        {
            Action act = () => new ConnectionInformation(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serverUri");
        }

        [TestMethod]
        [DataRow("http://localhost", "http://localhost/")]
        [DataRow("http://localhost/", "http://localhost/")]
        [DataRow("http://localhost:9000", "http://localhost:9000/")]
        [DataRow("https://localhost:9000/", "https://localhost:9000/")]
        [DataRow("https://local.sonarcloud.io", "https://local.sonarcloud.io/")]
        [DataRow("https://sonarcloud.io.local", "https://sonarcloud.io.local/")]
        public void Ctor_SonarQubeUrl_IsProcessedCorrectly(string inputUrl, string expectedUrl)
        {
            var testSubject = new ConnectionInformation(new Uri(inputUrl));

            testSubject.ServerUri.ToString().Should().Be(expectedUrl);
            testSubject.IsSonarCloud.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("http://sonarcloud.io") ]
        [DataRow("http://sonarcloud.io/") ]
        [DataRow("https://sonarcloud.io") ]
        [DataRow("https://sonarcloud.io/") ]
        [DataRow("http://SONARCLOUD.IO") ]
        [DataRow("http://www.sonarcloud.io") ]
        [DataRow("https://www.sonarcloud.io/") ]
        [DataRow("http://sonarcloud.io:9999") ]
        public void Ctor_SonarCloudUrl_IsProcessedCorrectly(string inputUrl)
        {
            var testSubject = new ConnectionInformation(new Uri(inputUrl));

            testSubject.ServerUri.Should().Be(ConnectionInformation.FixedSonarCloudUri);
            testSubject.IsSonarCloud.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("http://localhost", null, null, null)]
        [DataRow("https://sonarcloud.io", null, null, null)]
        [DataRow("http://localhost", "user1", "secret", null)]
        [DataRow("http://sonarcloud.io", null, null, "myorg")]
        [DataRow("http://sonarcloud.io", "a token", null, "myorg")]
        public void Clone_PropertiesAreCopiedCorrectly(string serverUrl, string userName, string password, string orgKey)
        {
            var securePwd = InitializeSecureString(password);
            var org = InitializeOrganization(orgKey);

            var testSubject = new ConnectionInformation(new Uri(serverUrl), userName, securePwd)
            {
                Organization = org
            };

            var cloneObj = ((ICloneable)testSubject).Clone();
            cloneObj.Should().BeOfType<ConnectionInformation>();

            CheckPropertiesMatch(testSubject, (ConnectionInformation)cloneObj);
        }

        [TestMethod]
        public void Dispose_PasswordIsDisposed()
        {
            var pwd = "secret".ToSecureString();
            var testSubject = new ConnectionInformation(new Uri("http://any"), "any", pwd);

            testSubject.Dispose();

            testSubject.IsDisposed.Should().BeTrue();

            Action accessPassword = () => _ = testSubject.Password.Length;
            accessPassword.Should().ThrowExactly<ObjectDisposedException>();
        }

        private static SecureString InitializeSecureString(string password) =>
            // The "ToSecureString" doesn't expect nulls, which we want to use in the tests
            password?.ToSecureString();

        private static SonarQubeOrganization InitializeOrganization(string orgKey) =>
            orgKey == null ? null : new SonarQubeOrganization(orgKey, Guid.NewGuid().ToString());

        private static void CheckPropertiesMatch(ConnectionInformation item1, ConnectionInformation item2)
        {
            item1.ServerUri.Should().Be(item2.ServerUri);
            item1.UserName.Should().Be(item2.UserName);
            item1.Organization.Should().Be(item2.Organization);


            if (item1.Password == null)
            {
                item2.Password.Should().BeNull();
            }
            else
            {
                item1.Password.ToUnsecureString().Should().Be(item2.Password.ToUnsecureString());
            }

            item1.Authentication.Should().Be(item2.Authentication);
            item1.IsSonarCloud.Should().Be(item2.IsSonarCloud);
        }
    }
}
