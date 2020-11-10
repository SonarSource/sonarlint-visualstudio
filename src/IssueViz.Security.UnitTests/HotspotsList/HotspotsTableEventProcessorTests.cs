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

using System.Reflection;
using System.Windows;
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.Events;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsTableEventProcessorTests
    {
        [TestMethod]
        public void KeyDown_KeyIsNotEnter_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            
            var testSubject = CreateTestSubject(Mock.Of<IWpfTableControl>(), locationNavigator.Object);
            testSubject.KeyDown(CreateKeyEventArgs(Key.Space));

            VerifyNoNavigation(locationNavigator);
        }

        [TestMethod]
        public void KeyDown_KeyIsEnter_NoSelectedEntry_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var tableControl = new Mock<IWpfTableControl>();
            tableControl.Setup(x => x.SelectedEntry).Returns((ITableEntryHandle) null);

            var testSubject = CreateTestSubject(tableControl.Object, locationNavigator.Object);
            testSubject.KeyDown(CreateKeyEventArgs(Key.Enter));

            VerifyNoNavigation(locationNavigator);
        }

        [TestMethod]
        public void KeyDown_KeyIsEnter_SelectedEntryIsNotIssueViz_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var tableControl = new Mock<IWpfTableControl>();
            tableControl.Setup(x => x.SelectedEntry).Returns(Mock.Of<ITableEntryHandle>());

            var testSubject = CreateTestSubject(tableControl.Object, locationNavigator.Object);
            testSubject.KeyDown(CreateKeyEventArgs(Key.Enter));

            VerifyNoNavigation(locationNavigator);
        }

        [TestMethod]
        public void KeyDown_KeyIsEnter_SelectedEntryIssueViz_NavigationToIssue()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            
            var entry = new Mock<ITableEntryHandle>();
            entry.Setup(x => x.Identity).Returns(issueViz);

            var tableControl = new Mock<IWpfTableControl>();
            tableControl.Setup(x => x.SelectedEntry).Returns(entry.Object);

            var testSubject = CreateTestSubject(tableControl.Object, locationNavigator.Object);
            testSubject.KeyDown(CreateKeyEventArgs(Key.Enter));

            VerifyNavigation(locationNavigator, issueViz);
        }

        [TestMethod]
        public void PostprocessMouseDown_SingleClick_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(Mock.Of<IWpfTableControl>(), locationNavigator.Object);
            testSubject.PostprocessMouseDown(Mock.Of<ITableEntryHandle>(), CreateMouseEventArgs(1));

            VerifyNoNavigation(locationNavigator);
        }

        [TestMethod]
        public void PostprocessMouseDown_DoubleClick_ClickedEntryIsNotIssueViz_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(Mock.Of<IWpfTableControl>(), locationNavigator.Object);
            testSubject.PostprocessMouseDown(Mock.Of<ITableEntryHandle>(), CreateMouseEventArgs(2));

            VerifyNoNavigation(locationNavigator);
        }

        [TestMethod]
        public void PostprocessMouseDown_DoubleClick_ClickedEntryIsIssueViz_NavigationToIssue()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();

            var entry = new Mock<ITableEntryHandle>();
            entry.Setup(x => x.Identity).Returns(issueViz);

            var testSubject = CreateTestSubject(Mock.Of<IWpfTableControl>(), locationNavigator.Object);
            testSubject.PostprocessMouseDown(entry.Object, CreateMouseEventArgs(2));

            VerifyNavigation(locationNavigator, issueViz);
        }

        private ITableControlEventProcessor CreateTestSubject(IWpfTableControl tableControl, ILocationNavigator locationNavigator)
            => new HotspotsTableEventProcessor(tableControl, locationNavigator);

        private KeyEventArgs CreateKeyEventArgs(Key key)
        {
            var eventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, Mock.Of<PresentationSource>(), 0, key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };

            return eventArgs;
        }

        private MouseButtonEventArgs CreateMouseEventArgs(int numberOfClicks)
        {
            var eventArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left);

            var fieldInfo = typeof(MouseButtonEventArgs).GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(eventArgs, numberOfClicks);

            return eventArgs;
        }

        private void VerifyNoNavigation(Mock<ILocationNavigator> locationNavigator)
        {
            locationNavigator.Invocations.Count.Should().Be(0);
        }

        private static void VerifyNavigation(Mock<ILocationNavigator> locationNavigator, IAnalysisIssueLocationVisualization issueViz)
        {
            locationNavigator.Verify(x => x.TryNavigate(issueViz), Times.Once);
            locationNavigator.VerifyNoOtherCalls();
        }
    }
}
