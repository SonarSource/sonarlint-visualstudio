/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual;

namespace SonarLint.VisualStudio.Education.UnitTests.Layout.Logical;

[TestClass]
public class RootCauseSectionTests
{
    [TestMethod]
    public void KeyAndTitle_AreCorrect()
    {
        var testSubject = new RootCauseSection("", false);
        var testSubjectHotspot = new RootCauseSection("", true);

        testSubject.Key.Should().BeSameAs(RootCauseSection.RuleInfoKey).And.Be("root_cause");
        testSubject.Title.Should().Be("Why is this an issue?");
        testSubjectHotspot.Key.Should().BeSameAs(RootCauseSection.RuleInfoKey).And.Be("root_cause");
        testSubjectHotspot.Title.Should().Be("What's the risk?");
    }

    [TestMethod]
    public void GetVisualizationTreeNode_ProducesOneContentSection()
    {
        var partialXaml = "<Paragraph>Your code is bad</Paragraph>";
        var testSubject = new RootCauseSection(partialXaml, false);

        var visualizationTreeNode = testSubject.GetVisualizationTreeNode(null);

        visualizationTreeNode.Should().BeOfType<ContentSection>().Which.content.Should().Be(partialXaml);
    }
}
