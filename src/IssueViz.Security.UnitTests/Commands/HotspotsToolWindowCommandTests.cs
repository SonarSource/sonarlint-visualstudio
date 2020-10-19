/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel.Design;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Commands
{
    [TestClass]
    public class HotspotsToolWindowCommandTests : ToolWindowCommandTests<HotspotsToolWindow>
    {
        public HotspotsToolWindowCommandTests() 
            : base(ExecuteCommand)
        {
        }

        private static void ExecuteCommand(AsyncPackage package, ILogger logger)
        {
            var testSubject = new HotspotsToolWindowCommand(package, Mock.Of<IMenuCommandService>(), logger);
            
            testSubject.Execute(null, EventArgs.Empty);
        }

        [TestMethod]
        public void Ctor_ArgsCheck()
        {
            var package = Mock.Of<AsyncPackage>();
            var commandService = Mock.Of<IMenuCommandService>();
            var logger = Mock.Of<ILogger>();

            Action act = () => new HotspotsToolWindowCommand(null, commandService, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("package");

            act = () => new HotspotsToolWindowCommand(package, null, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("commandService");

            act = () => new HotspotsToolWindowCommand(package, commandService, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_CommandAddedToMenu()
        {
            var commandService = new Mock<IMenuCommandService>();

            new HotspotsToolWindowCommand(Mock.Of<AsyncPackage>(), commandService.Object, Mock.Of<ILogger>());

            commandService.Verify(x =>
                    x.AddCommand(It.Is((MenuCommand c) =>
                        c.CommandID.Guid == HotspotsToolWindowCommand.CommandSet &&
                        c.CommandID.ID == HotspotsToolWindowCommand.ViewToolWindowCommandId)),
                Times.Once);

            commandService.VerifyNoOtherCalls();
        }
    }
}
