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

using SonarLint.VisualStudio.ConnectedMode.UI.DeleteConnection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.DeleteConnection;

[TestClass]
public class PreventDeleteConnectionViewModelTests
{
    [TestMethod]
    public void Ctor_SetsProperties()
    {
        var projectsToUnbind = Substitute.For<IReadOnlyList<string>>();
        var connectionInfo = new ConnectionInfo(default, default);

        var testSubject = new PreventDeleteConnectionViewModel(projectsToUnbind, connectionInfo);

        testSubject.ConnectionInfo.Should().BeSameAs(connectionInfo);
        testSubject.ProjectsToUnbind.Should().BeSameAs(projectsToUnbind);
    }

    [DataTestMethod]
    public void DisplayProjectList_MultipleProjectsToUnbind_ReturnsTrue()
    {
        var projects = new[] { "binding1", "binding2"};

        var testSubject = new PreventDeleteConnectionViewModel(projects, new ConnectionInfo(default, default));

        testSubject.DisplayProjectList.Should().BeTrue();
    }

    [DataTestMethod]
    public void DisplayProjectList_ProjectsIsNull_ReturnsFalse()
    {
        var testSubject = new PreventDeleteConnectionViewModel(null, new ConnectionInfo(default, default));

        testSubject.DisplayProjectList.Should().BeFalse();
    }

    [DataTestMethod]
    public void DisplayProjectList_NoProjectsToUnbind_ReturnsFalse()
    {
        var testSubject = new PreventDeleteConnectionViewModel([], new ConnectionInfo(default, default));

        testSubject.DisplayProjectList.Should().BeFalse();
    }
}
