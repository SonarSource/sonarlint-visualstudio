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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class IssueVisualizationControlTests
    {
        [TestMethod]
        public void IssueDescription_KeyDown_KeyIsNotEnter_NoNavigation()
        {
            var viewModel = new Mock<IIssueVisualizationViewModel>();
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(locationNavigator.Object, viewModel.Object);
            testSubject.IssueDescription_OnKeyDown(null, CreateKeyEventArgs(null, Key.Space));

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IssueDescription_KeyDown_KeyIsEnter_NavigationToCurrentIssue()
        {
            VerifyIssueNavigated(testSubject =>
                testSubject.IssueDescription_OnKeyDown(null, CreateKeyEventArgs(null, Key.Enter)));
        }

        [TestMethod]
        public void IssueDescription_MouseDown_NavigationToCurrentIssue()
        {
            VerifyIssueNavigated(testSubject => 
                testSubject.IssueDescription_OnMouseLeftButtonDown(null, null));
        }

        [TestMethod]
        public void LocationList_KeyDown_KeyIsNotEnter_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();
            var listItem = new LocationListItem(location);
            var mockSource = new ListViewItem { Content = listItem };

            var testSubject = CreateTestSubject(locationNavigator.Object);
            testSubject.LocationsList_OnKeyDown(null, CreateKeyEventArgs(mockSource, Key.Space));

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void LocationList_KeyDown_KeyIsEnter_NavigationToListItem()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();
            var listItem = new LocationListItem(location);
            var mockSource = new ListViewItem { Content = listItem };

            var testSubject = CreateTestSubject(locationNavigator.Object);
            testSubject.LocationsList_OnKeyDown(null, CreateKeyEventArgs(mockSource, Key.Enter));

            locationNavigator.Verify(x=> x.TryNavigate(location), Times.Once);
        }

        [TestMethod]
        public void LocationList_MouseDown_NavigationToListItem()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();
            var listItem = new LocationListItem(location);
            var mockSource = new TextBlock { DataContext = listItem };

            var testSubject = CreateTestSubject(locationNavigator.Object);
            testSubject.LocationsList_OnMouseLeftButtonDown(null, CreateMouseEventArgs(mockSource));

            locationNavigator.Verify(x => x.TryNavigate(location), Times.Once);
        }

        private void VerifyIssueNavigated(Action<IssueVisualization.IssueVisualizationControl.IssueVisualizationControl> simulateEvent)
        {
            var expectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            var viewModel = new Mock<IIssueVisualizationViewModel>();
            viewModel.SetupGet(x => x.CurrentIssue).Returns(expectedIssue);

            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(locationNavigator.Object, viewModel.Object);
            simulateEvent(testSubject);

            locationNavigator.Verify(x => x.TryNavigate(expectedIssue), Times.Once);
        }

        private IssueVisualization.IssueVisualizationControl.IssueVisualizationControl CreateTestSubject(ILocationNavigator locationNavigator, IIssueVisualizationViewModel viewModel = null)
        {
            viewModel = viewModel ?? Mock.Of<IIssueVisualizationViewModel>();
            return new IssueVisualization.IssueVisualizationControl.IssueVisualizationControl(viewModel, locationNavigator);
        }

        private KeyEventArgs CreateKeyEventArgs(object mockSource, Key key)
        {
            var eventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, Mock.Of<PresentationSource>(), 0, key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };
            eventArgs.Source = mockSource;

            return eventArgs;
        }

        private MouseButtonEventArgs CreateMouseEventArgs(object mockSource)
        {
            var eventArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
            {
                RoutedEvent = Mouse.MouseDownEvent
            };
            eventArgs.Source = mockSource;

            return eventArgs;
        }
    }
}
