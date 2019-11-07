/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class ManageConnectionsCommandTests
    {
        [TestMethod]
        public void ManageConnectionsCommand_Invoke()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            var teController = new ConfigurableTeamExplorerController();

            var testSubject = new ManageConnectionsCommand(teController);

            // Test case 1: was disabled
            command.Enabled = false;

            // Act
            using (new AssertIgnoreScope()) // Invoked when disabled
            {
                testSubject.Invoke(command, null);
            }

            // Assert
            teController.ShowConnectionsPageCallsCount.Should().Be(0);

            // Test case 2: was enabled
            command.Enabled = true;

            // Act
            testSubject.Invoke(command, null);

            // Assert
            teController.ShowConnectionsPageCallsCount.Should().Be(1);
        }

        [TestMethod]
        public void ManageConnectionsCommand_QueryStatus()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            // Test case 1: no TE controller
            // Arrange
            command.Enabled = false;

            var testSubject1 = new ManageConnectionsCommand(null);
          
            // Act
            testSubject1.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected the command to be disabled on QueryStatus when no TE controller");

            // Test case 2: has TE controller
            // Arrange
            var teController = new ConfigurableTeamExplorerController();
            var testSubject2 = new ManageConnectionsCommand(teController);

            // Act
            testSubject2.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeTrue("Expected the command to be disabled on QueryStatus when does have TE controller");
        }

    }
}
