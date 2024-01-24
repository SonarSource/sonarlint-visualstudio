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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl.ViewModelCommands
{
    [TestClass]
    public class NavigateToCodeLocationCommandTests
    {
        [TestMethod]
        public void NavigateCommand_CanExecute_NullParameter_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var testSubject = CreateTestSubject(locationNavigator.Object);

            var result = testSubject.CanExecute(null);
            result.Should().BeFalse();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_ParameterIsNotLocationVisualization_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var testSubject = CreateTestSubject(locationNavigator.Object);

            var result = testSubject.CanExecute(new object());
            result.Should().BeFalse();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_ParameterIsLocationVisualization_True()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var testSubject = CreateTestSubject(locationNavigator.Object);

            var result = testSubject.CanExecute(Mock.Of<IAnalysisIssueLocationVisualization>());
            result.Should().BeTrue();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_Execute_LocationNavigated()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var locationViz = Mock.Of<IAnalysisIssueLocationVisualization>();

            var testSubject = CreateTestSubject(locationNavigator.Object);
            testSubject.Execute(locationViz);

            locationNavigator.Verify(x => x.TryNavigate(locationViz), Times.Once);
            locationNavigator.VerifyNoOtherCalls();
        }

        private NavigateToCodeLocationCommand CreateTestSubject(ILocationNavigator locationNavigator = null)
        {
            return new NavigateToCodeLocationCommand(locationNavigator);
        }
    }
}
