//-----------------------------------------------------------------------
// <copyright file="SonarQubeNavigationItemTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class SonarQubeNavigationItemTests
    {
        [TestMethod]
        public void SonarQubeNavigationItem_Execute()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var controller = new ConfigurableTeamExplorerController();

            var testSubject = new SonarQubeNavigationItem(controller);

            // Act
            testSubject.Execute();

            // Verify
            controller.AssertExpectedNumCallsShowConnectionsPage(1);
        }

        [TestMethod]
        public void SonarQubeNavigationItem_Ctor()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var controller = new ConfigurableTeamExplorerController();

            // Act
            var testSubject = new SonarQubeNavigationItem(controller);

            // Verify
            Assert.IsTrue(testSubject.IsVisible, "Nav item should be visible");
            Assert.IsTrue(testSubject.IsEnabled, "Nav item should be enabled");
            Assert.AreEqual(Strings.TeamExplorerPageTitle, testSubject.Text, "Unexpected nav text");

            Assert.IsNotNull(testSubject.Icon, "Icon should not be null");
        }

        [TestMethod]
        public void SonarQubeNavigationItem_Ctor_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SonarQubeNavigationItem(null));
        }
    }
}
