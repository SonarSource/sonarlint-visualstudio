/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class RuleHelpTests
    {
        
        // [TestMethod]
        // public void Ctor_SetsProperties()
        // {
        //     var context = new Context("some context key", "some display name");
        //     var descriptionSection1 = new DescriptionSection("some descriptionSection key 1", "some htmlcontent 1", context);
        //     var descriptionSection2 = new DescriptionSection("some descriptionSection key 2", "some htmlcontent 2");
        //     var descriptionSections = new[] { descriptionSection1, descriptionSection2 };
        //
        //     var educationPrinciples = new[] { "defense_in_depth", "never_trust_user_input" };
        //     var defaultImpacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();
        //     defaultImpacts.Add(SoftwareQuality.Maintainability, SoftwareQualitySeverity.Medium);
        //     defaultImpacts.Add(SoftwareQuality.Reliability, SoftwareQualitySeverity.Low);
        //
        //     var tags = new string[] { "convention", "bad-practice" };
        //
        //     var testSubject = new RuleInfo(
        //         Language.CSharp.ServerLanguage.Key,
        //         "xxx:S123",
        //         "a description",
        //         "the rule name",
        //         RuleIssueSeverity.Blocker,
        //         RuleIssueType.Vulnerability,
        //         isActiveByDefault: true,
        //         tags,
        //         descriptionSections,
        //         educationPrinciples,
        //         "some user note",
        //         CleanCodeAttribute.Respectful,
        //         defaultImpacts);
        //
        //     testSubject.LanguageKey.Should().Be(Language.CSharp.ServerLanguage.Key);
        //     testSubject.FullRuleKey.Should().Be("xxx:S123");
        //     testSubject.Description.Should().Be("a description");
        //     testSubject.Name.Should().Be("the rule name");
        //     testSubject.Severity.Should().Be(RuleIssueSeverity.Blocker);
        //     testSubject.IssueType.Should().Be(RuleIssueType.Vulnerability);
        //     testSubject.IsActiveByDefault.Should().BeTrue();
        //     testSubject.Tags.Should().BeEquivalentTo(tags);
        //     testSubject.DescriptionSections.Should().BeEquivalentTo(descriptionSections);
        //     testSubject.EducationPrinciples.Should().BeEquivalentTo(educationPrinciples);
        //     testSubject.HtmlNote.Should().Be("some user note");
        //     testSubject.CleanCodeAttribute.Should().Be(CleanCodeAttribute.Respectful);
        //     testSubject.DefaultImpacts.Should().BeEquivalentTo(defaultImpacts);
        // }
    }
}
