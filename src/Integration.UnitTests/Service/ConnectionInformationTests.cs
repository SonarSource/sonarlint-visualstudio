///*
// * SonarLint for Visual Studio
// * Copyright (C) 2016-2025 SonarSource SA
// * mailto:info AT sonarsource DOT com
// *
// * This program is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 3 of the License, or (at your option) any later version.
// *
// * This program is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public License
// * along with this program; if not, write to the Free Software Foundation,
// * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// */

//TODO by https://sonarsource.atlassian.net/browse/SLVS-1816 Fix these test class once SonarQube.Client has reference to Core assembly

//using SonarLint.VisualStudio.ConnectedMode.Persistence;
//using SonarLint.VisualStudio.TestInfrastructure;
//using SonarQube.Client.Helpers;
//using SonarQube.Client.Models;

//namespace SonarLint.VisualStudio.Integration.UnitTests
//{
//    [TestClass]
//    public class ConnectionInformationTests
//    {
//        [TestMethod]
//        public void ConnectionInformation_WithLoginInformation()
//        {
//            // Arrange
//            var userName = "admin";
//            var passwordUnsecure = "admin";
//            var password = passwordUnsecure.ToSecureString();
//            var serverUri = new Uri("http://localhost/");
//            var credentials = new UsernameAndPasswordCredentials(userName, password);
//            var testSubject = new ConnectionInformation(serverUri, credentials);

//            // Act
//            password.Dispose(); // Connection information should maintain it's own copy of the password

//            // Assert
//            ((UsernameAndPasswordCredentials)testSubject.Credentials).Password.ToUnsecureString().Should().Be(passwordUnsecure, "Password doesn't match");
//            ((UsernameAndPasswordCredentials)testSubject.Credentials).UserName.Should().Be(userName, "UserName doesn't match");
//            testSubject.ServerUri.Should().Be(serverUri, "ServerUri doesn't match");

//            // Act clone
//            var testSubject2 = (ConnectionInformation)((ICloneable)testSubject).Clone();

//            // Now dispose the test subject
//            testSubject.Dispose();

//            // Assert testSubject
//            Exceptions.Expect<ObjectDisposedException>(() => ((UsernameAndPasswordCredentials)testSubject.Credentials).Password.ToUnsecureString());

//            // Assert testSubject2
//            ((UsernameAndPasswordCredentials)testSubject2.Credentials).Password.ToUnsecureString().Should().Be(passwordUnsecure, "Password doesn't match");
//            ((UsernameAndPasswordCredentials)testSubject.Credentials).UserName.Should().Be(userName, "UserName doesn't match");
//            testSubject2.ServerUri.Should().Be(serverUri, "ServerUri doesn't match");
//        }

//        [TestMethod]
//        public void ConnectionInformation_WithoutLoginInformation()
//        {
//            // Arrange
//            var serverUri = new Uri("http://localhost/");

//            // Act
//            var testSubject = new ConnectionInformation(serverUri, null);

//            // Assert
//            testSubject.Credentials.Should().BeAssignableTo<INoCredentials>();
//            testSubject.ServerUri.Should().Be(serverUri, "ServerUri doesn't match");

//            // Act clone
//            var testSubject2 = (ConnectionInformation)((ICloneable)testSubject).Clone();

//            // Assert testSubject2
//            testSubject2.Credentials.Should().BeAssignableTo<INoCredentials>();
//            testSubject2.ServerUri.Should().Be(serverUri, "ServerUri doesn't match");
//        }

//        [TestMethod]
//        public void ConnectionInformation_Ctor_NormalizesServerUri()
//        {
//            // Act
//            var noSlashResult = new ConnectionInformation(new Uri("http://localhost/NoSlash"));

//            // Assert
//            noSlashResult.ServerUri.ToString().Should().Be("http://localhost/NoSlash/", "Unexpected normalization of URI without trailing slash");
//        }

//        [TestMethod]
//        public void ConnectionInformation_Ctor_FixesSonarCloudUri()
//        {
//            new ConnectionInformation(new Uri("http://sonarcloud.io")).ServerUri.ToString().Should().Be("https://sonarcloud.io/");
//            new ConnectionInformation(new Uri("http://www.sonarcloud.io")).ServerUri.ToString().Should().Be("https://sonarcloud.io/");
//            new ConnectionInformation(new Uri("https://www.sonarcloud.io")).ServerUri.ToString().Should().Be("https://sonarcloud.io/");
//            new ConnectionInformation(new Uri("https://WWW.SONARCLOUD.IO")).ServerUri.ToString().Should().Be("https://sonarcloud.io/");
//            new ConnectionInformation(new Uri("http://us.sonarcloud.io")).ServerUri.ToString().Should().Be("https://us.sonarcloud.io/");
//            new ConnectionInformation(new Uri("http://www.us.sonarcloud.io")).ServerUri.ToString().Should().Be("https://us.sonarcloud.io/");
//            new ConnectionInformation(new Uri("https://www.us.sonarcloud.io")).ServerUri.ToString().Should().Be("https://us.sonarcloud.io/");
//            new ConnectionInformation(new Uri("https://WWW.us.SONARCLOUD.IO")).ServerUri.ToString().Should().Be("https://us.sonarcloud.io/");
//        }

//        [TestMethod]
//        public void ConnectionInformation_Ctor_ArgChecks()
//        {
//            Exceptions.Expect<ArgumentNullException>(() => new ConnectionInformation(null));
//            Exceptions.Expect<ArgumentNullException>(() => new ConnectionInformation(null, new UsernameAndPasswordCredentials("user", "pwd".ToSecureString())));
//        }
//    }
//}


