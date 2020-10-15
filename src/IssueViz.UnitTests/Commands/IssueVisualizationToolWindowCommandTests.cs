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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Commands;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Helpers;
using IVsMonitorSelection = Microsoft.VisualStudio.Shell.Interop.IVsMonitorSelection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Commands
{
    [TestClass]
    public class IssueVisualizationToolWindowCommandTests : ToolWindowCommandTests<IssueVisualizationToolWindow>
    {
        public IssueVisualizationToolWindowCommandTests() 
            : base(ExecuteCommand)
        {
        }

        private static void ExecuteCommand(AsyncPackage package, ILogger logger)
        {
            var testSubject = new IssueVisualizationToolWindowCommand(package, Mock.Of<IMenuCommandService>(), Mock.Of<IVsMonitorSelection>(), logger);

            testSubject.Execute(null, EventArgs.Empty);
        }

        [TestMethod]
        public void Ctor_ArgsCheck()
        {
            var package = Mock.Of<AsyncPackage>();
            var commandService = Mock.Of<IMenuCommandService>();
            var monitorSelection = Mock.Of<IVsMonitorSelection>();
            var logger = Mock.Of<ILogger>();

            Action act = () => new IssueVisualizationToolWindowCommand(null, commandService, monitorSelection, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("package");

            act = () => new IssueVisualizationToolWindowCommand(package, null, monitorSelection, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("commandService");

            act = () => new IssueVisualizationToolWindowCommand(package, commandService, null, logger);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("monitorSelection");

            act = () => new IssueVisualizationToolWindowCommand(package, commandService, monitorSelection, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_CommandAddedToMenu()
        {
            var commandService = new Mock<IMenuCommandService>();

            new IssueVisualizationToolWindowCommand(Mock.Of<AsyncPackage>(),
                commandService.Object,
                Mock.Of<IVsMonitorSelection>(),
                Mock.Of<ILogger>());

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
            uint uiContextCookie = 999;
            var uiContext = new Guid(IssueVisualization.Commands.Constants.UIContextGuid);
            var monitorSelection = new Mock<IVsMonitorSelection>();
            monitorSelection.Setup(x => x.GetCmdUIContextCookie(ref uiContext, out uiContextCookie)).Returns(VSConstants.S_OK);

            var result = 1;
            monitorSelection
                .Setup(x => x.IsCmdUIContextActive(uiContextCookie, out result))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new Mock<ILogger>();

            var testSubject = new IssueVisualizationToolWindowCommand(Mock.Of<AsyncPackage>(),
                Mock.Of<IMenuCommandService>(),
                monitorSelection.Object,
                logger.Object);

            Action act = () => testSubject.ErrorListQueryStatus(null, EventArgs.Empty);
            act.Should().NotThrow();

            logger.Verify(x =>
                    x.WriteLine(It.Is((string message) => message.Contains("this is a test"))),
                Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ErrorListQueryStatus_CommandVisibilityIsSetToContextActivation(bool isContextActive)
        {
            uint uiContextCookie = 999;
            var uiContext = new Guid(IssueVisualization.Commands.Constants.UIContextGuid);
            var monitorSelection = new Mock<IVsMonitorSelection>();
            monitorSelection.Setup(x => x.GetCmdUIContextCookie(ref uiContext, out uiContextCookie)).Returns(VSConstants.S_OK);

            var isActive = isContextActive ? 1 : 0;
            monitorSelection.Setup(x => x.IsCmdUIContextActive(uiContextCookie, out isActive)).Returns(VSConstants.S_OK);

            var testSubject = new IssueVisualizationToolWindowCommand(Mock.Of<AsyncPackage>(),
                Mock.Of<IMenuCommandService>(),
                monitorSelection.Object,
                Mock.Of<ILogger>());

            testSubject.ErrorListMenuItem.Visible = !isContextActive;

            testSubject.ErrorListQueryStatus(null, EventArgs.Empty);

            testSubject.ErrorListMenuItem.Visible.Should().Be(isContextActive);
        }
    }
}
