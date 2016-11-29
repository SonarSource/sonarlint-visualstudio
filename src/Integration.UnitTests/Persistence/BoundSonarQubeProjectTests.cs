//-----------------------------------------------------------------------
// <copyright file="BoundSonarQubeProjectTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BoundSonarQubeProjectTests
    {
        [TestMethod]
        public void BoundSonarQubeProject_Serialization()
        {
            // Setup
            var serverUri = new Uri("https://finding-nemo.org");
            var projectKey = "MyProject Key";
            var testSubject = new BoundSonarQubeProject(serverUri, projectKey, new BasicAuthCredentials("used", "pwd".ToSecureString()));

            // Act (serialize + de-serialize)
            string data = JsonHelper.Serialize(testSubject);
            BoundSonarQubeProject deserialized = JsonHelper.Deserialize<BoundSonarQubeProject>(data);

            // Verify
            Assert.AreNotSame(testSubject, deserialized);
            Assert.AreEqual(testSubject.ProjectKey, deserialized.ProjectKey);
            Assert.AreEqual(testSubject.ServerUri, deserialized.ServerUri);
            Assert.IsNull(deserialized.Credentials);
        }
    }
}
