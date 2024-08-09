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

using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ProjectSelection;

[TestClass]
public class ProjectSelectionViewModelTests
{
    private static readonly List<ServerProject> AnInitialListOfProjects =
    [
        new ServerProject("a-project", "A Project"),
        new ServerProject("another-project", "Another Project")
    ];

    private static readonly ConnectionInfo.Connection AConnection = new("http://localhost:9000",
        ConnectionInfo.ServerType.SonarQube, true);
    
    private ProjectSelectionViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new ProjectSelectionViewModel(AConnection);
    }

    [TestMethod]
    public void IsProjectSelected_NoProjectSelected_ReturnsFalse()
    {
        testSubject.IsProjectSelected.Should().BeFalse();
    }
    
    [TestMethod]
    public void IsProjectSelected_ProjectSelected_ReturnsTrue()
    {
        testSubject.SelectedProject = new ServerProject("a-project", "A Project");
        
        testSubject.IsProjectSelected.Should().BeTrue();
    }
    
    [TestMethod]
    public void InitProjects_ResetsTheProjectResults()
    {
        testSubject.InitProjects(AnInitialListOfProjects);
        testSubject.ProjectResults.Should().BeEquivalentTo(AnInitialListOfProjects);
        
        var updatedListOfProjects = new List<ServerProject>
        {
            new("new-project", "New Project")
        };
        testSubject.InitProjects(updatedListOfProjects);
        testSubject.ProjectResults.Should().BeEquivalentTo(updatedListOfProjects);
    }

    [TestMethod]
    public void ProjectSearchTerm_WithEmptyTerm_ShouldNotUpdateSearchResult()
    {
        testSubject.InitProjects(AnInitialListOfProjects);

        testSubject.ProjectSearchTerm = "";

        testSubject.ProjectResults.Should().BeEquivalentTo(AnInitialListOfProjects);
    }

    [TestMethod]
    public void ProjectSearchTerm_WithTerm_ShouldUpdateSearchResult()
    {
        testSubject.InitProjects(AnInitialListOfProjects);

        testSubject.ProjectSearchTerm = "My Project";

        testSubject.ProjectResults.Should().NotContain(AnInitialListOfProjects);
    }
}
