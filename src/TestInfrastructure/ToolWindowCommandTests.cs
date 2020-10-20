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
using System.Collections.Generic;
using System.ComponentModel.Design;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public abstract class ToolWindowCommandTests<T> where T : ToolWindowPane
    {
        private readonly Action<AsyncPackage, ILogger> executeCommand;
        private readonly Func<IMenuCommandService, object> createCommand;
        private readonly IDictionary<Guid, IEnumerable<int>> commandsInCommandSet;

        private Mock<AsyncPackage> package;
        private Mock<ILogger> logger;

        protected ToolWindowCommandTests(Action<AsyncPackage, ILogger> executeCommand, 
            Func<IMenuCommandService, object> createCommand, 
            IDictionary<Guid, IEnumerable<int>> commandsInCommandSet)
        {
            this.executeCommand = executeCommand;
            this.createCommand = createCommand;
            this.commandsInCommandSet = commandsInCommandSet;

            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            package = new Mock<AsyncPackage>();
            logger = new Mock<ILogger>();
        }

        [TestMethod]
        public void Ctor_CommandAddedToMenu()
        {
            var commandService = new Mock<IMenuCommandService>();

            createCommand(commandService.Object);

            foreach (var commandInCommandSet in commandsInCommandSet)
            {
                var commandSetId = commandInCommandSet.Key;
                var commandIds = commandInCommandSet.Value;

                foreach (var commandId in commandIds)
                {
                    commandService.Verify(x =>
                            x.AddCommand(It.Is((MenuCommand c) =>
                                c.CommandID.Guid == commandSetId &&
                                c.CommandID.ID == commandId)),
                        Times.Once);
                }
            }

            commandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Execute_WindowNotCreated_NoException()
        {
            SetupWindowCreation(null);

            VerifyExecutionDoesNotThrow();

            VerifyMessageLogged("cannot create window frame");
        }

        [TestMethod]
        public void Execute_WindowCreatedWithoutFrame_NoException()
        {
            SetupWindowCreation(new ToolWindowPane());

            VerifyExecutionDoesNotThrow();

            VerifyMessageLogged("cannot create window frame");
        }

        [TestMethod]
        public void Execute_NonCriticalExceptionInCreatingWindow_IsSuppressed()
        {
            SetupWindowCreation(new ToolWindowPane { Frame = Mock.Of<IVsWindowFrame>() }, new NotImplementedException("this is a test"));

            VerifyExecutionDoesNotThrow();

            VerifyMessageLogged("this is a test");
        }

        [TestMethod]
        public void Execute_WindowCreatedWithIVsWindowFrame_WindowIsShown()
        {
            var frame = new Mock<IVsWindowFrame>();
            SetupWindowCreation(new ToolWindowPane { Frame = frame.Object });

            VerifyExecutionDoesNotThrow();

            frame.Verify(x => x.Show(), Times.Once);

            logger.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Execute_WindowCreatedWithIVsWindowFrame_ExceptionInShowingWindow_NoException()
        {
            var frame = new Mock<IVsWindowFrame>();
            frame.Setup(x => x.Show()).Throws(new NotImplementedException("this is a test"));

            SetupWindowCreation(new ToolWindowPane { Frame = frame.Object });

            VerifyExecutionDoesNotThrow();

            frame.Verify(x => x.Show(), Times.Once);

            VerifyMessageLogged("this is a test");
        }

        private void SetupWindowCreation(WindowPane windowPane, Exception exceptionToThrow = null)
        {
            var setup = package.Protected().Setup<WindowPane>("CreateToolWindow", typeof(T), 0);

            if (exceptionToThrow == null)
            {
                setup.Returns(windowPane);
            }
            else
            {
                setup.Throws(exceptionToThrow);
            }
        }

        private void VerifyMessageLogged(string expectedMessage)
        {
            logger.Verify(x =>
                    x.WriteLine(It.Is((string message) => message.Contains(expectedMessage))),
                Times.Once);
        }

        private void VerifyExecutionDoesNotThrow()
        {
            Action act = () => executeCommand(package.Object, logger.Object);
            act.Should().NotThrow();
        }
    }
}
