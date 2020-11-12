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

using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;
using DataTrigger = System.Windows.DataTrigger;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotTableEntryWpfElementFactoryTests
    {
        [TestMethod]
        public void Create_CreatesElementWithContextMenu()
        {
            var testSubject = CreateTestSubject();
            var frameworkElement = testSubject.Create("some content");

            frameworkElement.Should().NotBeNull();
            frameworkElement.ContextMenu.Should().NotBeNull();
            frameworkElement.ContextMenu.Items.Count.Should().Be(1);

            var removeMenuItem = frameworkElement.ContextMenu.Items[0] as MenuItem;
            removeMenuItem.Header.Should().Be("Remove");
        }

        [TestMethod]
        public void Create_ChildElement_IssueVizIsNavigable_CreatesElementWithoutStyle()
        {
            // Arrange
            var vsUiShell = new Mock<IVsUIShell2>();

            // Act
            var testSubject = CreateTestSubject(vsUiShell.Object, true);
            var frameworkElement = testSubject.Create("some content");

            // Assert
            vsUiShell.VerifyNoOtherCalls();
            frameworkElement.Should().BeOfType<Border>();

            var textBlock = (frameworkElement as Border).Child as TextBlock;
            textBlock.Should().NotBeNull();

            textBlock.Text.Should().Be("some content");
            textBlock.FontStyle.Should().Be(FontStyles.Normal);
            textBlock.Style.Should().BeNull();
        }

        [TestMethod]
        public void Create_ChildElement_IssueVizIsNotNavigable_CreatesElementWithNavigabilityStyle()
        {
            // Arrange
            var selectedTextColor = (uint)Color.Yellow.ToArgb();
            var notSelectedTextColor = (uint)Color.Red.ToArgb();

            var vsUiShell = new Mock<IVsUIShell2>();
            vsUiShell.Setup(x => x.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_COMMANDBAR_TEXT_INACTIVE, out notSelectedTextColor));
            vsUiShell.Setup(x => x.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_COMMANDBAR_TEXT_SELECTED, out selectedTextColor));

            // Act
            var testSubject = CreateTestSubject(vsUiShell.Object, false);
            var frameworkElement = testSubject.Create("some content");

            // Assert
            frameworkElement.Should().BeOfType<Border>();
            var textBlock = (frameworkElement as Border).Child as TextBlock;
            textBlock.Should().NotBeNull();

            textBlock.Text.Should().Be("some content");
            textBlock.FontStyle.Should().Be(FontStyles.Italic);
            textBlock.Style.Should().NotBeNull();
            textBlock.Style.Triggers.Count.Should().Be(2);
            textBlock.Style.Triggers.Should().AllBeOfType<DataTrigger>();

            var itemSelectedTrigger = textBlock.Style.Triggers.First() as DataTrigger;
            itemSelectedTrigger.Value.Should().Be(true);

            var itemNotSelectedTrigger = textBlock.Style.Triggers.Last() as DataTrigger;
            itemNotSelectedTrigger.Value.Should().Be(false);

            VerifyStyling(itemSelectedTrigger, Brushes.Yellow);
            VerifyStyling(itemNotSelectedTrigger, Brushes.Red);
        }

        private void VerifyStyling(DataTrigger trigger, Brush expectedColor)
        {
            (trigger.Binding as Binding).Path.Path.Should().Be("IsSelected");
            trigger.Setters.Should().NotBeNull();
            (trigger.Setters.First() as Setter).Property.Should().Be(TextBlock.ForegroundProperty);
            (trigger.Setters.First() as Setter).Value.Should().BeEquivalentTo(expectedColor, c => c.ExcludingMissingMembers());
        }

        private static HotspotTableEntryWpfElementFactory CreateTestSubject(IVsUIShell2 vsUiShell = null, bool isNavigable = true)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            if (!isNavigable)
            {
                issueViz.SetupGet(x => x.Span).Returns(new SnapshotSpan());
            }

            vsUiShell ??= Mock.Of<IVsUIShell2>();

            return new HotspotTableEntryWpfElementFactory(vsUiShell, issueViz.Object);
        }
    }
}
