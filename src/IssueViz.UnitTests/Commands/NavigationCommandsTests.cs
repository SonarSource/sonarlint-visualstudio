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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Commands;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using ThreadHelper = SonarLint.VisualStudio.Integration.UnitTests.ThreadHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Commands
{
    [TestClass]
    public class NavigationCommandsTests
    {
        private Mock<IMenuCommandService> commandService;
        private Mock<ILogger> logger;
        private Mock<IAnalysisIssueNavigation> navigationService;

        [TestInitialize]
        public void TestInitialize()
        {
            commandService = new Mock<IMenuCommandService>();
            navigationService = new Mock<IAnalysisIssueNavigation>();
            logger = new Mock<ILogger>();

            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void Ctor_ArgsCheck()
        {
            Action act = () => new NavigationCommands(null, navigationService.Object, logger.Object);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("commandService");

            act = () => new NavigationCommands(commandService.Object, null, logger.Object);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("analysisIssueNavigation");

            act = () => new NavigationCommands(commandService.Object, navigationService.Object, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_CommandsAddedToMenu()
        {
            new NavigationCommands(commandService.Object, navigationService.Object, logger.Object);

            commandService.Verify(x =>
                    x.AddCommand(It.Is((MenuCommand c) =>
                        c.CommandID.Guid == NavigationCommands.CommandSet &&
                        c.CommandID.ID == NavigationCommands.NextLocationCommandId)),
                Times.Once);

            commandService.Verify(x =>
                    x.AddCommand(It.Is((MenuCommand c) =>
                        c.CommandID.Guid == NavigationCommands.CommandSet &&
                        c.CommandID.ID == NavigationCommands.PreviousLocationCommandId)),
                Times.Once);

            commandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteGotoNextLocation_CallsNavigationService()
        {
            var testSubject = new NavigationCommands(commandService.Object, navigationService.Object, logger.Object);

            testSubject.ExecuteGotoNextLocation(null, null);

            navigationService.Verify(x=> x.GotoNextLocation(), Times.Once);
            navigationService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteGotoPreviousLocation_CallsNavigationService()
        {
            var testSubject = new NavigationCommands(commandService.Object, navigationService.Object, logger.Object);

            testSubject.ExecuteGotoPreviousLocation(null, null);

            navigationService.Verify(x => x.GotoPreviousLocation(), Times.Once);
            navigationService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteGotoNextLocation_Exception_ExceptionLogged()
        {
            navigationService
                .Setup(x => x.GotoNextLocation())
                .Throws(new NotImplementedException("this is a test"));

            var testSubject = new NavigationCommands(commandService.Object, navigationService.Object, logger.Object);

            Action act = () => testSubject.ExecuteGotoNextLocation(null, null);
            act.Should().NotThrow();

            VerifyMessageLogged("this is a test");
        }


        [TestMethod]
        public void ExecuteGotoPreviousLocation_Exception_ExceptionLogged()
        {
            navigationService
                .Setup(x => x.GotoPreviousLocation())
                .Throws(new NotImplementedException("this is a test"));

            var testSubject = new NavigationCommands(commandService.Object, navigationService.Object, logger.Object);

            Action act = () => testSubject.ExecuteGotoPreviousLocation(null, null);
            act.Should().NotThrow();

            VerifyMessageLogged("this is a test");
        }

        private void VerifyMessageLogged(string expectedMessage)
        {
            logger.Verify(x =>
                    x.WriteLine(It.Is((string message) => message.Contains(expectedMessage))),
                Times.Once);
        }
    }
}
