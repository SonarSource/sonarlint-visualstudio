//-----------------------------------------------------------------------
// <copyright file="BoundSonarQubeProjectExtensionsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
            var creds = new BasicAuthCredentials("UserName", "password".ConvertToSecureString());
            var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey", creds);

            // Act
            ConnectionInformation conn = input.CreateConnectionInformation();

            // Verify
            Assert.AreEqual(input.ServerUri, conn.ServerUri);
            Assert.AreEqual(creds.UserName, conn.UserName);
            Assert.AreEqual(creds.Password.ConvertToUnsecureString(), conn.Password.ConvertToUnsecureString());
        }
    }
}
