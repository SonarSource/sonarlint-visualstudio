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
        private Mock<IMenuCommandService> commandServiceMock;
        private Mock<ILogger> loggerMock;
        private Mock<IIssueFlowStepNavigator> issueFlowStepNavigatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            commandServiceMock = new Mock<IMenuCommandService>();
            issueFlowStepNavigatorMock = new Mock<IIssueFlowStepNavigator>();
            loggerMock = new Mock<ILogger>();

            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void Ctor_ArgsCheck()
        {
            Action act = () => new NavigationCommands(null, issueFlowStepNavigatorMock.Object, loggerMock.Object);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("commandService");

            act = () => new NavigationCommands(commandServiceMock.Object, null, loggerMock.Object);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueFlowStepNavigator");

            act = () => new NavigationCommands(commandServiceMock.Object, issueFlowStepNavigatorMock.Object, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_CommandsAddedToMenu()
        {
            new NavigationCommands(commandServiceMock.Object, issueFlowStepNavigatorMock.Object, loggerMock.Object);

            commandServiceMock.Verify(x =>
                    x.AddCommand(It.Is((MenuCommand c) =>
                        c.CommandID.Guid == NavigationCommands.CommandSet &&
                        c.CommandID.ID == NavigationCommands.NextLocationCommandId)),
                Times.Once);

            commandServiceMock.Verify(x =>
                    x.AddCommand(It.Is((MenuCommand c) =>
                        c.CommandID.Guid == NavigationCommands.CommandSet &&
                        c.CommandID.ID == NavigationCommands.PreviousLocationCommandId)),
                Times.Once);

            commandServiceMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteGotoNextNavigableFlowStep_CallsNavigationService()
        {
            var testSubject = new NavigationCommands(commandServiceMock.Object, issueFlowStepNavigatorMock.Object, loggerMock.Object);

            testSubject.ExecuteGotoNextNavigableFlowStep(null, null);

            issueFlowStepNavigatorMock.Verify(x=> x.GotoNextNavigableFlowStep(), Times.Once);
            issueFlowStepNavigatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteGotoPreviousNavigableFlowStep_CallsNavigationService()
        {
            var testSubject = new NavigationCommands(commandServiceMock.Object, issueFlowStepNavigatorMock.Object, loggerMock.Object);

            testSubject.ExecuteGotoPreviousNavigableFlowStep(null, null);

            issueFlowStepNavigatorMock.Verify(x => x.GotoPreviousNavigableFlowStep(), Times.Once);
            issueFlowStepNavigatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteGotoNextNavigableFlowStep_Exception_ExceptionLogged()
        {
            issueFlowStepNavigatorMock
                .Setup(x => x.GotoNextNavigableFlowStep())
                .Throws(new NotImplementedException("this is a test"));

            var testSubject = new NavigationCommands(commandServiceMock.Object, issueFlowStepNavigatorMock.Object, loggerMock.Object);

            Action act = () => testSubject.ExecuteGotoNextNavigableFlowStep(null, null);
            act.Should().NotThrow();

            VerifyMessageLogged("this is a test");
        }


        [TestMethod]
        public void ExecuteGotoPreviousNavigableFlowStep_Exception_ExceptionLogged()
        {
            issueFlowStepNavigatorMock
                .Setup(x => x.GotoPreviousNavigableFlowStep())
                .Throws(new NotImplementedException("this is a test"));

            var testSubject = new NavigationCommands(commandServiceMock.Object, issueFlowStepNavigatorMock.Object, loggerMock.Object);

            Action act = () => testSubject.ExecuteGotoPreviousNavigableFlowStep(null, null);
            act.Should().NotThrow();

            VerifyMessageLogged("this is a test");
        }

        private void VerifyMessageLogged(string expectedMessage)
        {
            loggerMock.Verify(x =>
                    x.WriteLine(It.Is((string message) => message.Contains(expectedMessage))),
                Times.Once);
        }
    }
}
