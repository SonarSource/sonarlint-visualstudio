//-----------------------------------------------------------------------
// <copyright file="TeamExplorerControllerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class TeamExplorerControllerTests
    {
        [TestMethod]
        public void TeamExplorerController_Ctor_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new TeamExplorerController(null));
        }

        [TestMethod]
        public void TeamExplorerController_Ctor()
        {
            // Test case 1: no Team Explorer service
            // Setup
            var serviceProvider = new ConfigurableServiceProvider(false);

            // Act + Verify
            Exceptions.Expect<ArgumentException>(() => new TeamExplorerController(serviceProvider));

            // Test case 2: has TE service
            // Setup
            var teService = new ConfigurableTeamExplorer();
            serviceProvider.RegisterService(typeof(ITeamExplorer), teService);

            // Act + Verify
            var testSubject = new TeamExplorerController(serviceProvider);
            Assert.AreSame(teService, testSubject.TeamExplorer, "Unexpected Team Explorer service");
        }

        [TestMethod]
        public void TeamExplorerController_ShowConnectionsPage()
        {
            // Setup
            var startPageId = new Guid(TeamExplorerPageIds.GitCommits);

            var serviceProvider = new ConfigurableServiceProvider();
            var teService = new ConfigurableTeamExplorer(startPageId);
            serviceProvider.RegisterService(typeof(ITeamExplorer), teService);

            var sonarPageId = new Guid(SonarQubePage.PageId);
            var sonarPageInstance = new ConfigurableTeamExplorerPage(sonarPageId);
            teService.AvailablePages.Add(sonarPageId, sonarPageInstance);

            var testSubject = new TeamExplorerController(serviceProvider);

            // Act
            testSubject.ShowConnectionsPage();

            // Verify
            teService.AssertCurrentPage(sonarPageId);
        }
    }
}
