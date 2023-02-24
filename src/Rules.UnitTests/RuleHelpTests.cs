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
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class RuleHelpTests
    {
        [TestMethod]
        public void Context_Ctor_SetsProperties()
        {
            var testSubject = new Context("some key", "some display name");
            testSubject.Key.Should().Be("some key");
            testSubject.DisplayName.Should().Be("some display name");
        }

        [TestMethod]
        public void DescriptionSection_Ctor_SetsProperties()
        {
            var context1 = new Context("some context key 1", "some display name 1");
            var context2 = new Context("some context key 2", "some display name 2");
            var context = new[] { context1, context2 };
            var testSubject = new DescriptionSection("some descriptionSection key", "some htmlcontent", context);
            testSubject.Key.Should().Be("some descriptionSection key");
            testSubject.HtmlContent.Should().Be("some htmlcontent");
            testSubject.Context.Should().BeEquivalentTo(context);
        }

        [TestMethod]
        public void DescriptionSection_NoContext_Ctor_SetsProperties()
        {
            var testSubject = new DescriptionSection("some descriptionSection key", "some htmlcontent");
            testSubject.Key.Should().Be("some descriptionSection key");
            testSubject.HtmlContent.Should().Be("some htmlcontent");
            testSubject.Context.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_SetsProperties()
        {
            var context1 = new Context("some context key 1", "some display name 1");
            var context2 = new Context("some context key 2", "some display name 2");
            var context = new[] { context1, context2 };
            var descriptionSection1 = new DescriptionSection("some descriptionSection key 1", "some htmlcontent 1", context);
            var descriptionSection2 = new DescriptionSection("some descriptionSection key 2", "some htmlcontent 2");
            var descriptionSections = new[] { descriptionSection1, descriptionSection2 };

            var educationPrinciples = new[] { "defense_in_depth", "never_trust_user_input" };

            var tags = new string[] { "convention", "bad-practice" };

            var testSubject = new RuleInfo(
                Language.CSharp.ServerLanguage.Key,
                "xxx:S123", 
                "a description", 
                "the rule name",
                RuleIssueSeverity.Blocker,
                RuleIssueType.Vulnerability,
                isActiveByDefault: true,
                tags, 
                descriptionSections,
                educationPrinciples);

            testSubject.LanguageKey.Should().Be(Language.CSharp.ServerLanguage.Key);
            testSubject.FullRuleKey.Should().Be("xxx:S123");
            testSubject.Description.Should().Be("a description");
            testSubject.Name.Should().Be("the rule name");
            testSubject.DefaultSeverity.Should().Be(RuleIssueSeverity.Blocker);
            testSubject.IssueType.Should().Be(RuleIssueType.Vulnerability);
            testSubject.IsActiveByDefault.Should().BeTrue();
            testSubject.Tags.Should().BeEquivalentTo(tags);
            testSubject.DescriptionSections.Should().BeEquivalentTo(descriptionSections);
            testSubject.EducationPrinciples.Should().BeEquivalentTo(educationPrinciples);
        }
    }
}
