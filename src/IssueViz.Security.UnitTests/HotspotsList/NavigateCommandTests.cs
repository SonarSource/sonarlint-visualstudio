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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList2.ViewModels;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class NavigateCommandTests
    {
        [TestMethod]
        public void CanExecute_NullParameter_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = new NavigateCommand(locationNavigator.Object);
            var result = testSubject.CanExecute(null);
            result.Should().BeFalse();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CanExecute_ParameterIsNotHotspotViewModel_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = new NavigateCommand(locationNavigator.Object);
            var result = testSubject.CanExecute("something");
            result.Should().BeFalse();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CanExecute_ParameterIsHotspotViewModel_True()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = new NavigateCommand(locationNavigator.Object);
            var result = testSubject.CanExecute(Mock.Of<IHotspotViewModel>());
            result.Should().BeTrue();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Execute_LocationNavigated()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var hotspot = Mock.Of<IAnalysisIssueVisualization>();
            var viewModel = new Mock<IHotspotViewModel>();
            viewModel.Setup(x => x.Hotspot).Returns(hotspot);

            var testSubject = new NavigateCommand(locationNavigator.Object);
            testSubject.Execute(viewModel.Object);

            locationNavigator.Verify(x=> x.TryNavigate(hotspot), Times.Once);
            locationNavigator.VerifyNoOtherCalls();
        }
    }
}
