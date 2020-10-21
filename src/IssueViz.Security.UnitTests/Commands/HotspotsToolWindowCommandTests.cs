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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Security.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Commands
{
    [TestClass]
    public class HotspotsToolWindowCommandTests : ToolWindowCommandTests<HotspotsToolWindow>
    {
        protected override Guid CommandSetId => HotspotsToolWindowCommand.CommandSet;
        protected override IEnumerable<int> CommandIds => new[] {HotspotsToolWindowCommand.ViewToolWindowCommandId};

        protected override object CreateCommand(IMenuCommandService commandService) =>
            new HotspotsToolWindowCommand(Mock.Of<AsyncPackage>(), commandService, Mock.Of<ILogger>());

        protected override void ExecuteCommand(AsyncPackage package, ILogger logger)
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
    }

    public abstract class ToolWindowCommandTests<T> where T : ToolWindowPane
    {
        protected abstract Guid CommandSetId { get; }
        protected abstract IEnumerable<int> CommandIds { get; }
        protected abstract void ExecuteCommand(AsyncPackage package, ILogger logger);
        protected abstract object CreateCommand(IMenuCommandService commandService);

        private Mock<AsyncPackage> package;
        private Mock<ILogger> logger;

        [TestInitialize]
        public void TestInitialize()
        {
            package = new Mock<AsyncPackage>();
            logger = new Mock<ILogger>();

            SetCurrentThreadAsUIThread();
        }

        private static void SetCurrentThreadAsUIThread()
        {
            var methodInfo = typeof(Microsoft.VisualStudio.Shell.ThreadHelper).GetMethod("SetUIThread", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            methodInfo.Should().NotBeNull("Could not find ThreadHelper.SetUIThread");
            methodInfo.Invoke(null, null);
        }

        [TestMethod]
        public void Ctor_CommandAddedToMenu()
        {
            var commandService = new Mock<IMenuCommandService>();

            CreateCommand(commandService.Object);

            foreach (var commandId in CommandIds)
            {
                commandService.Verify(x =>
                        x.AddCommand(It.Is((MenuCommand c) =>
                            c.CommandID.Guid == CommandSetId &&
                            c.CommandID.ID == commandId)),
                    Times.Once);
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
            Action act = () => ExecuteCommand(package.Object, logger.Object);
            act.Should().NotThrow();
        }
    }
}
