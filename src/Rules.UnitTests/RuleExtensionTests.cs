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
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class RuleExtensionTests
    {
        [TestMethod]
        public void GetCompositeKey_GetsKey()
        {
            var testSubject = new SonarQubeRule("key", "repositoryKey", true, SonarQubeIssueSeverity.Info, null, SonarQubeIssueType.Unknown, null, null, null, null, null, null);

            var result = testSubject.GetCompositeKey();

            result.Should().Be("repositoryKey:key");
        }

        [DataRow(SonarQubeIssueSeverity.Blocker, RuleIssueSeverity.Blocker)]
        [DataRow(SonarQubeIssueSeverity.Critical, RuleIssueSeverity.Critical)]
        [DataRow(SonarQubeIssueSeverity.Info, RuleIssueSeverity.Info)]
        [DataRow(SonarQubeIssueSeverity.Major, RuleIssueSeverity.Major)]
        [DataRow(SonarQubeIssueSeverity.Minor, RuleIssueSeverity.Minor)]
        [DataRow(SonarQubeIssueSeverity.Unknown, RuleIssueSeverity.Unknown)]
        [TestMethod]
        public void ToRuleIssueSeverity_Converts(SonarQubeIssueSeverity input, RuleIssueSeverity expectedResult)
        {
            var actualResult = input.ToRuleIssueSeverity();

            actualResult.Should().Be(expectedResult);
        }

        [DataRow(SonarQubeIssueType.Bug, RuleIssueType.Bug)]
        [DataRow(SonarQubeIssueType.CodeSmell, RuleIssueType.CodeSmell)]
        [DataRow(SonarQubeIssueType.SecurityHotspot, RuleIssueType.Hotspot)]
        [DataRow(SonarQubeIssueType.Unknown, RuleIssueType.Unknown)]
        [DataRow(SonarQubeIssueType.Vulnerability, RuleIssueType.Vulnerability)]
        [TestMethod]
        public void ToRuleIssueType_Converts(SonarQubeIssueType input, RuleIssueType expectedResult)
        {
            var actualResult = input.ToRuleIssueType();

            actualResult.Should().Be(expectedResult);
        }

        [TestMethod]
        public void ToDescriptionSection_HasContext_Converts()
        {
            var testSubject = new SonarQubeDescriptionSection("Key", "htmlContent", new SonarQubeContext("DisplayName", "ContextKey"));

            var result = testSubject.ToDescriptionSection();

            result.Key.Should().Be("Key");
            result.HtmlContent.Should().Be("htmlContent");

            result.Context.Should().NotBeNull();
            result.Context.Key.Should().Be("ContextKey");
            result.Context.DisplayName.Should().Be("DisplayName");
        }

        [TestMethod]
        public void ToDescriptionSection_HasNotContext_ConvertsWithNullContext()
        {
            var testSubject = new SonarQubeDescriptionSection("Key", "htmlContent", null);

            var result = testSubject.ToDescriptionSection();

            result.Key.Should().Be("Key");
            result.HtmlContent.Should().Be("htmlContent");

            result.Context.Should().BeNull();
        }
    }
}
