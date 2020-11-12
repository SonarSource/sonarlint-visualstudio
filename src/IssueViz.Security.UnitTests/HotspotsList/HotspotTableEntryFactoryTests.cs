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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotTableEntryFactoryTests
    {
        [TestMethod]
        public void Create_NullIssueViz_ArgumentNullException()
        {
            var testSubject = new HotspotTableEntryFactory(Mock.Of<IServiceProvider>());

            Action act = () => testSubject.Create(null, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualization");
        }

        [TestMethod]
        public void Create_BaseIssueIsNotHotspot_InvalidCastException()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(Mock.Of<IAnalysisIssueBase>());

            var testSubject = new HotspotTableEntryFactory(Mock.Of<IServiceProvider>());

            Action act = () => testSubject.Create(issueViz.Object, null);
            act.Should().Throw<InvalidCastException>();
        }

        [TestMethod]
        public void Create_BaseIssueIsHotspot_ReturnsTableEntry()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(Mock.Of<IHotspot>());

            var testSubject = new HotspotTableEntryFactory(Mock.Of<IServiceProvider>());
            var result = testSubject.Create(issueViz.Object, null);

            result.Should().NotBeNull();
            result.Should().BeOfType<HotspotTableEntry>();
        }
    }
}
