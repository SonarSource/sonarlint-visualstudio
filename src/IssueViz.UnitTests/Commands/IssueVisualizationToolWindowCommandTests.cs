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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Commands;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using ThreadHelper = SonarLint.VisualStudio.Integration.UnitTests.ThreadHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Commands
{
    [TestClass]
    public class IssueVisualizationToolWindowCommandTests
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
            var monitorSelection = Mock.Of<IVsMonitorSelection>();
            var logger = Mock.Of<ILogger>();

            Action act = () => new IssueVisualizationToolWindowCommand(null, commandService, monitorSelection, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("toolWindowService");

            act = () => new IssueVisualizationToolWindowCommand(toolWindowService, null, monitorSelection, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("commandService");

            act = () => new IssueVisualizationToolWindowCommand(toolWindowService, commandService, null, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("monitorSelection");

            act = () => new IssueVisualizationToolWindowCommand(toolWindowService, commandService, monitorSelection, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_CommandAddedToMenu()
        {
            var commandService = new Mock<IMenuCommandService>();

            new IssueVisualizationToolWindowCommand(Mock.Of<IToolWindowService>(), commandService.Object, Mock.Of<IVsMonitorSelection>(), Mock.Of<ILogger>());

            commandService.Verify(x =>
                    x.AddCommand(It.Is((MenuCommand c) =>
                        c.CommandID.Guid == IssueVisualizationToolWindowCommand.CommandSet &&
                        c.CommandID.ID == IssueVisualizationToolWindowCommand.ViewToolWindowCommandId)),
                Times.Once);

            commandService.Verify(x =>
                    x.AddCommand(It.Is((OleMenuCommand c) =>
                        c.CommandID.Guid == IssueVisualizationToolWindowCommand.CommandSet &&
                        c.CommandID.ID == IssueVisualizationToolWindowCommand.ErrorListCommandId)),
                Times.Once);

            commandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ErrorListQueryStatus_NonCriticalException_IsSuppressed()
        {
            var logger = new TestLogger(logToConsole: true);

            const uint uiContextCookie = 123;
            var monitorSelection = new Mock<IVsMonitorSelection>();
            SetupSelectionService(monitorSelection, uiContextCookie);

            var result = 1;
            monitorSelection
                .Setup(x => x.IsCmdUIContextActive(uiContextCookie, out result))
                .Throws(new NotImplementedException("this is a test"));

            var testSubject = new IssueVisualizationToolWindowCommand(Mock.Of<IToolWindowService>(), Mock.Of<IMenuCommandService>(),
                monitorSelection.Object, logger);

            Action act = () => testSubject.ErrorListQueryStatus(null, EventArgs.Empty);
            act.Should().NotThrow();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ErrorListQueryStatus_CommandVisibilityIsSetToContextActivation(bool isContextActive)
        {
            const uint uiContextCookie = 999;
            var monitorSelection = new Mock<IVsMonitorSelection>();
            SetupSelectionService(monitorSelection, uiContextCookie);

            var isActive = isContextActive ? 1 : 0;
            monitorSelection.Setup(x => x.IsCmdUIContextActive(uiContextCookie, out isActive)).Returns(VSConstants.S_OK);

            var testSubject = new IssueVisualizationToolWindowCommand(Mock.Of<IToolWindowService>(), Mock.Of<IMenuCommandService>(),
                monitorSelection.Object, Mock.Of<ILogger>());

            testSubject.ErrorListMenuItem.Visible = !isContextActive;

            testSubject.ErrorListQueryStatus(null, EventArgs.Empty);

            testSubject.ErrorListMenuItem.Visible.Should().Be(isContextActive);
        }

        [TestMethod]
        public void Execute_ServiceCalled()
        {
            var logger = new TestLogger(logToConsole: true);
            var toolwindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = new IssueVisualizationToolWindowCommand(toolwindowServiceMock.Object,
                Mock.Of<IMenuCommandService>(), Mock.Of<IVsMonitorSelection>(), logger);

            // Act
            testSubject.Execute(null, null);

            toolwindowServiceMock.Verify(x => x.Show(IssueVisualizationToolWindow.ToolWindowId), Times.Once);
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Execute_NonCriticalException_IsSuppressed()
        {
            var logger = new TestLogger(logToConsole: true);
            var toolwindowServiceMock = new Mock<IToolWindowService>();
            toolwindowServiceMock.Setup(x => x.Show(IssueVisualizationToolWindow.ToolWindowId)).Throws(new InvalidOperationException("thrown by test"));

            var testSubject = new IssueVisualizationToolWindowCommand(toolwindowServiceMock.Object,
                Mock.Of<IMenuCommandService>(), Mock.Of<IVsMonitorSelection>(), logger);

            // Act
            testSubject.Execute(null, null);

            toolwindowServiceMock.Verify(x => x.Show(IssueVisualizationToolWindow.ToolWindowId), Times.Once);
            logger.AssertPartialOutputStringExists("thrown by test");
        }

        [TestMethod]
        public void Execute_CriticalException_IsNotSuppressed()
        {
            var logger = new TestLogger(logToConsole: true);
            var toolwindowServiceMock = new Mock<IToolWindowService>();
            toolwindowServiceMock.Setup(x => x.Show(IssueVisualizationToolWindow.ToolWindowId)).Throws(new StackOverflowException("thrown by test"));

            var testSubject = new IssueVisualizationToolWindowCommand(toolwindowServiceMock.Object,
                Mock.Of<IMenuCommandService>(), Mock.Of<IVsMonitorSelection>(), logger);

            // Act
            Action act = () => testSubject.Execute(null, null);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("thrown by test");
            logger.AssertNoOutputMessages();
        }

        private void SetupSelectionService(Mock<IVsMonitorSelection> monitorSelection, uint uiContextCookie)
        {
            var uiContext = new Guid(IssueVisualization.Commands.Constants.UIContextGuid);
            monitorSelection.Setup(x => x.GetCmdUIContextCookie(ref uiContext, out uiContextCookie)).Returns(VSConstants.S_OK);
        }
    }
}
