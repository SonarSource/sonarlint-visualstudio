﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl;

[TestClass]
public class NavigateToRuleDescriptionCommandConverterTests
{
    private readonly NavigateToRuleDescriptionCommandConverter testSubject = new NavigateToRuleDescriptionCommandConverter();

    [TestMethod]
    public void Convert_WrongNumberOfParams_ReturnsNull()
    {
        var values = new object[] { "RuleKey", "context", "third param" };

        var command = testSubject.Convert(values, default, default, default);

        command.Should().BeNull();
    }

    [DataRow("str", 3)]
    [DataRow(3, "str")]
    [DataRow(3, 3)]
    [TestMethod]
    public void Convert_WrongTypes_ReturnsNull(object param1, object param2)
    {
        var values = new object[] { param1, param2 };

        var command = testSubject.Convert(values, default, default, default);

        command.Should().BeNull();
    }

    [TestMethod]
    public void ConvertBack_CorrectType_ReturnsNull()
    {
        var command = new NavigateToRuleDescriptionCommandParam { FullRuleKey = "FullRuleKey" };

        var values = testSubject.ConvertBack(command, default, default, default);

        values.Should().BeNull();
    }

    [TestMethod]
    public void ConvertBack_WrongType_ReturnsValues()
    {
        var value = "Some str object";

        var values = testSubject.ConvertBack(value, default, default, default);

        values.Should().BeNull();
    }

    [TestMethod]
    public void Convert_WithIssueId_SetsIssueId()
    {
        var issuedId = Guid.NewGuid();
        var values = new object[] { "RuleKey", issuedId };

        var command = testSubject.Convert(values, default, default, default);

        var navigateToRuleDescriptionParam = command as NavigateToRuleDescriptionCommandParam;
        navigateToRuleDescriptionParam.Should().NotBeNull();
        navigateToRuleDescriptionParam.FullRuleKey.Should().Be("RuleKey");
        navigateToRuleDescriptionParam.IssueId.Should().Be(issuedId);
    }

    [TestMethod]
    public void Convert_WithoutIssueId_SetsIssueIdToNull()
    {
        var values = new object[] { "RuleKey", null };

        var command = testSubject.Convert(values, default, default, default);

        var navigateToRuleDescriptionParam = command as NavigateToRuleDescriptionCommandParam;
        navigateToRuleDescriptionParam.Should().NotBeNull();
        navigateToRuleDescriptionParam.FullRuleKey.Should().Be("RuleKey");
        navigateToRuleDescriptionParam.IssueId.Should().BeNull();
    }
}
