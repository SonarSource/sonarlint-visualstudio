/*
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

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests;

[TestClass]
public class SuppressedIssueTests
{
    [TestMethod]
    public void AreSame_ReturnsTrueIfPropertiesHaveSameValue()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();
        var suppressedIssue2 = TestHelper.CreateIssue();

        suppressedIssue1.AreSame(suppressedIssue2).Should().BeTrue();
    }

    [TestMethod]
    public void AreSame_ReturnsFalseIfRuleIdIsDifferent()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();
        var suppressedIssue2 = TestHelper.CreateIssue(ruleId: "S666");

        suppressedIssue1.AreSame(suppressedIssue2).Should().BeFalse();
    }

    [TestMethod]
    public void AreSame_ReturnsFalseIfFilePathIsDifferent()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();
        var suppressedIssue2 = TestHelper.CreateIssue(path: "differentPath");

        suppressedIssue1.AreSame(suppressedIssue2).Should().BeFalse();
    }

    [TestMethod]
    public void AreSame_ReturnsFalseIfLineIsDifferent()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();
        var suppressedIssue2 = TestHelper.CreateIssue(line: suppressedIssue1.RoslynIssueLine + 1);

        suppressedIssue1.AreSame(suppressedIssue2).Should().BeFalse();
    }

    [TestMethod]
    public void AreSame_ReturnsFalseIfHashIsDifferent()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();
        var suppressedIssue2 = TestHelper.CreateIssue(hash: "differentHash");

        suppressedIssue1.AreSame(suppressedIssue2).Should().BeFalse();
    }

    [TestMethod]
    public void AreSame_ReturnsFalseIfLanguageIsDifferent()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();
        var suppressedIssue2 = TestHelper.CreateIssue(language: RoslynLanguage.VB);

        suppressedIssue1.AreSame(suppressedIssue2).Should().BeFalse();
    }

    [TestMethod]
    public void AreSame_ReturnsFalseIfIssueServerKeyIsDifferent()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();
        var suppressedIssue2 = TestHelper.CreateIssue(issueServerKey: "key6");

        suppressedIssue1.AreSame(suppressedIssue2).Should().BeFalse();
    }

    [TestMethod]
    public void AreSame_ReturnsFalseIfSuppressedIssueIsNull()
    {
        var suppressedIssue1 = TestHelper.CreateIssue();

        suppressedIssue1.AreSame(null).Should().BeFalse();
    }
}
