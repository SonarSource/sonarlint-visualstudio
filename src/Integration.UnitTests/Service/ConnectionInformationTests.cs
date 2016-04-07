//-----------------------------------------------------------------------
// <copyright file="ConnectionInformationTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
            var password = passwordUnsecure.ConvertToSecureString();
            var serverUri = new Uri("http://localhost/");
            var testSubject = new ConnectionInformation(serverUri, userName, password);

            // Act
            password.Dispose(); // Connection information should maintain it's own copy of the password

            // Verify
            Assert.AreEqual(passwordUnsecure, testSubject.Password.ConvertToUnsecureString(), "Password doesn't match");
            Assert.AreEqual(userName, testSubject.UserName, "UserName doesn't match");
            Assert.AreEqual(serverUri, testSubject.ServerUri, "ServerUri doesn't match");

            // Act clone
            var testSubject2 = (ConnectionInformation)((ICloneable)testSubject).Clone();

            // Now dispose the test subject
            testSubject.Dispose();

            // Verify testSubject
            Exceptions.Expect<ObjectDisposedException>(() => testSubject.Password.ConvertToUnsecureString());

            // Verify testSubject2
            Assert.AreEqual(passwordUnsecure, testSubject2.Password.ConvertToUnsecureString(), "Password doesn't match");
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
            Assert.AreEqual("http://localhost/NoSlash/", noSlashResult.ServerUri.ToString(), "Unexpected normalisation of URI without trailing slash");        }
    }
}
