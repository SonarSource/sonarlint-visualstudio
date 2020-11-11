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
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.CustomColumns;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class NavigabilityTableColumnDefinitionTests
    {
        [TestMethod]
        public void Ctor_PropertiesAreSet()
        {
            var testSubject = new NavigabilityTableColumnDefinition();

            testSubject.IsSortable.Should().BeFalse();
            testSubject.IsFilterable.Should().BeFalse();
            testSubject.DisplayName.Should().BeNullOrEmpty();
            testSubject.Name.Should().Be(NavigabilityTableColumnDefinition.ColumnName);
        }

        [TestMethod]
        public void TryCreateImageContent_NotIssueVizEntry_ReturnsNull()
        {
            var testSubject = new NavigabilityTableColumnDefinition();

            var result = testSubject.TryCreateImageContent(Mock.Of<ITableEntryHandle>(), true, out var content);
            result.Should().BeFalse();
            content.Should().BeEquivalentTo(default(ImageMoniker));
        }

        [TestMethod]
        public void TryCreateImageContent_IssueVizIsNavigable_ReturnsNull()
        {
            var entry = new Mock<ITableEntryHandle>();
            entry.Setup(x => x.Identity).Returns(CreateIssueViz(true));

            var testSubject = new NavigabilityTableColumnDefinition();

            var result = testSubject.TryCreateImageContent(entry.Object, true, out var content);
            result.Should().BeFalse();
            content.Should().BeEquivalentTo(default(ImageMoniker));
        }

        [TestMethod]
        public void TryCreateImageContent_IssueVizIsNotNavigable_ReturnsWarningIcon()
        {
            var entry = new Mock<ITableEntryHandle>();
            entry.Setup(x => x.Identity).Returns(CreateIssueViz(false));

            var testSubject = new NavigabilityTableColumnDefinition();

            var result = testSubject.TryCreateImageContent(entry.Object, true, out var content);
            result.Should().BeTrue();
            content.Should().BeEquivalentTo(KnownMonikers.DocumentWarning);
        }

        [TestMethod]
        public void TryCreateToolTip_NotIssueVizEntry_ReturnsNull()
        {
            var testSubject = new NavigabilityTableColumnDefinition();

            var result = testSubject.TryCreateToolTip(Mock.Of<ITableEntryHandle>(), out var content);
            result.Should().BeFalse();
            content.Should().BeNull();
        }

        [TestMethod]
        public void TryCreateToolTip_IssueVizIsNavigable_ReturnsNull()
        {
            var entry = new Mock<ITableEntryHandle>();
            entry.Setup(x => x.Identity).Returns(CreateIssueViz(true));

            var testSubject = new NavigabilityTableColumnDefinition();

            var result = testSubject.TryCreateToolTip(entry.Object, out var content);
            result.Should().BeFalse();
            content.Should().BeNull();
        }

        [TestMethod]
        public void TryCreateToolTip_IssueVizIsNotNavigable_ReturnsTooltip()
        {
            var entry = new Mock<ITableEntryHandle>();
            entry.Setup(x => x.Identity).Returns(CreateIssueViz(false));

            var testSubject = new NavigabilityTableColumnDefinition();

            var result = testSubject.TryCreateToolTip(entry.Object, out var content);
            result.Should().BeTrue();
            content.Should().NotBeNull();
        }

        private IAnalysisIssueVisualization CreateIssueViz(bool isNavigable)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupProperty(x => x.Span);

            if (!isNavigable)
            {
                issueViz.Object.Span = new SnapshotSpan();
            }

            return issueViz.Object;
        }
    }
}
