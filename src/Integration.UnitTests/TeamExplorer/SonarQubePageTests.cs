//-----------------------------------------------------------------------
// <copyright file="SonarQubePageTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class SonarQubePageTests
    {
        [TestMethod]
        public void SonarQubePageTests_Ctor()
        {
            // Act
            var testSubject = new SonarQubePage();

            // Verify
            Assert.AreEqual(Strings.TeamExplorerPageTitle, testSubject.Title, "Unexpected TE page title");
        }
    }
}
