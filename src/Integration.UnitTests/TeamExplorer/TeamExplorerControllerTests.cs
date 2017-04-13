/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System;
using FluentAssertions;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.TeamExplorer;

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
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider(false);

            // Act + Assert
            Exceptions.Expect<ArgumentException>(() => new TeamExplorerController(serviceProvider));

            // Test case 2: has TE service
            // Arrange
            var teService = new ConfigurableTeamExplorer();
            serviceProvider.RegisterService(typeof(ITeamExplorer), teService);

            // Act + Assert
            var testSubject = new TeamExplorerController(serviceProvider);
            testSubject.TeamExplorer.Should().Be(teService, "Unexpected Team Explorer service");
        }

        [TestMethod]
        public void TeamExplorerController_ShowConnectionsPage()
        {
            // Arrange
            var startPageId = new Guid(TeamExplorerPageIds.GitCommits);

            var serviceProvider = new ConfigurableServiceProvider();
            var teService = new ConfigurableTeamExplorer(startPageId);
            serviceProvider.RegisterService(typeof(ITeamExplorer), teService);

            var sonarPageId = new Guid(SonarQubePage.PageId);
            var sonarPageInstance = new ConfigurableTeamExplorerPage(sonarPageId);
            teService.AvailablePages.Add(sonarPageId, sonarPageInstance);

            var testSubject = new TeamExplorerController(serviceProvider);

            // Act
            testSubject.ShowSonarQubePage();

            // Assert
            teService.CurrentPageId.Should().Be(sonarPageId);
        }
    }
}