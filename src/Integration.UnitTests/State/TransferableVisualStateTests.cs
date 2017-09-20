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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.UnitTests.State
{
    [TestClass]
    public class TransferableVisualStateTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void TransferableVisualState_DefaultState()
        {
            // Arrange
            var testSubject = new TransferableVisualState();

            // Assert
            testSubject.HasBoundProject.Should().BeFalse();
            testSubject.IsBusy.Should().BeFalse();
            testSubject.ConnectedServers.Should().NotBeNull();
            testSubject.ConnectedServers.Should().BeEmpty();
        }

        [TestMethod]
        public void TransferableVisualState_BoundProjectManagement()
        {
            // Arrange
            var testSubject = new TransferableVisualState();
            var server = new ServerViewModel(new Integration.Service.ConnectionInformation(new System.Uri("http://server")));
            var project1 = new ProjectViewModel(server, new Integration.Service.SonarQubeProject());
            var project2 = new ProjectViewModel(server, new Integration.Service.SonarQubeProject());

            // Act (bind to something)
            testSubject.SetBoundProject(project1);

            // Assert
            testSubject.HasBoundProject.Should().BeTrue();
            project1.IsBound.Should().BeTrue();
            project2.IsBound.Should().BeFalse();
            server.ShowAllProjects.Should().BeFalse();

            // Act (bind to something else)
            testSubject.SetBoundProject(project2);

            // Assert
            testSubject.HasBoundProject.Should().BeTrue();
            project1.IsBound.Should().BeFalse();
            project2.IsBound.Should().BeTrue();
            server.ShowAllProjects.Should().BeFalse();

            // Act(clear binding)
            testSubject.ClearBoundProject();

            // Assert
            testSubject.HasBoundProject.Should().BeFalse();
            project1.IsBound.Should().BeFalse();
            project2.IsBound.Should().BeFalse();
            server.ShowAllProjects.Should().BeTrue();
        }
    }
}