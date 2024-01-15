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

using System;
using System.ComponentModel.Design;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Education.Commands;

using ThreadHelper = SonarLint.VisualStudio.TestInfrastructure.ThreadHelper;
using SonarLint.VisualStudio.Education;

namespace Education.UnitTests.Commands
{
    [TestClass]
    public class RuleHelpWindowCommandTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void Ctor_ArgsCheck()
        {
            var toolWindowService = Mock.Of<IToolWindowService>();
            var commandService = Mock.Of<IMenuCommandService>();
            var logger = Mock.Of<ILogger>();
            
            Action act = () => new RuleHelpWindowCommand(null, toolWindowService, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("commandService");

            act = () => new RuleHelpWindowCommand(commandService, null, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("toolWindowService");

            act = () => new RuleHelpWindowCommand(commandService, toolWindowService, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_CommandAddedToMenu()
        {
            var commandService = new Mock<IMenuCommandService>();

            new RuleHelpWindowCommand(commandService.Object, Mock.Of<IToolWindowService>(), Mock.Of<ILogger>());

            var guid = new Guid("80127033-1819-4996-8C45-E9C96F75E2A8");
            var id = 0x100;

            commandService.Verify(x =>
                    x.AddCommand(It.Is((MenuCommand c) =>
                        c.CommandID.Guid == guid &&
                        c.CommandID.ID == id)),
                Times.Once);

            commandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Execute_ServiceCalled()
        {
            var logger = new TestLogger(logToConsole: true);
            var toolwindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = new RuleHelpWindowCommand(Mock.Of<IMenuCommandService>(), toolwindowServiceMock.Object, logger);

            // Act
            testSubject.Execute(null, null);

            toolwindowServiceMock.Verify(x => x.Show(RuleHelpToolWindow.ToolWindowId), Times.Once);
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Execute_NonCriticalException_IsSuppressed()
        {
            var logger = new TestLogger(logToConsole: true);
            var toolwindowServiceMock = new Mock<IToolWindowService>();
            toolwindowServiceMock.Setup(x => x.Show(RuleHelpToolWindow.ToolWindowId)).Throws(new InvalidOperationException("thrown by test"));

            var testSubject = new RuleHelpWindowCommand(Mock.Of<IMenuCommandService>(), toolwindowServiceMock.Object, logger);

            // Act
            testSubject.Execute(null, null);

            toolwindowServiceMock.Verify(x => x.Show(RuleHelpToolWindow.ToolWindowId), Times.Once);
            logger.AssertPartialOutputStringExists("thrown by test");
        }

        [TestMethod]
        public void Execute_CriticalException_IsNotSuppressed()
        {
            var logger = new TestLogger(logToConsole: true);
            var toolwindowServiceMock = new Mock<IToolWindowService>();
            toolwindowServiceMock.Setup(x => x.Show(RuleHelpToolWindow.ToolWindowId)).Throws(new StackOverflowException("thrown by test"));

            var testSubject = new RuleHelpWindowCommand(Mock.Of<IMenuCommandService>(), toolwindowServiceMock.Object, logger);

            // Act
            Action act = () => testSubject.Execute(null, null);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("thrown by test");
            logger.AssertNoOutputMessages();
        }
    }
}
