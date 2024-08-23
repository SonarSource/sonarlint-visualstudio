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

using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding;

[TestClass]
public class BoundServerProjectTests
{
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void Ctor_InvalidLocalBinding_ThrowsArgumentNullException(string bindingName)
    {
        var act = () => new BoundServerProject(bindingName, "projectName", new ServerConnection.SonarCloud("org"));
        
        act.Should().Throw<ArgumentNullException>();
    }
    
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void Ctor_InvalidServerProject_ThrowsArgumentNullException(string projectName)
    {
        var act = () => new BoundServerProject("binding", projectName, new ServerConnection.SonarCloud("org"));
        
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Ctor_NullConnection_ThrowsArgumentNullException()
    {
        var act = () => new BoundServerProject("bindingName", "projectName", null);
        
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Ctor_SetsValues()
    {
        var localBindingKey = "bindingName";
        var serverProjectKey = "projectName";
        var serverConnection = new ServerConnection.SonarCloud("org");
        var boundServerProject = new BoundServerProject(localBindingKey, serverProjectKey, serverConnection);

        boundServerProject.LocalBindingKey.Should().BeSameAs(localBindingKey);
        boundServerProject.ServerProjectKey.Should().BeSameAs(serverProjectKey);
        boundServerProject.ServerConnection.Should().BeSameAs(serverConnection);
        boundServerProject.Profiles.Should().BeNull();
    }
}
