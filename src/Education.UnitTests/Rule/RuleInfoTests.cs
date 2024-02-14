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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.Education.UnitTests.Rule
{
    [TestClass]
    public class RuleInfoTests
    {
        [TestMethod]
        public void Ctor_NullCollectionsAreAllowed_SetToEmptyCollections()
        {
            var testSubject = new RuleInfo(
                languageKey: null,
                fullRuleKey: null,
                description: null,
                name: null,
                RuleIssueSeverity.Unknown,
                RuleIssueType.Unknown,
                isActiveByDefault: false,
                tags: null,
                educationPrinciples: null,
                htmlNote: null,
                richRuleDescriptionDto: null,
                cleanCodeAttribute: null,
                defaultImpacts: null);
        
            testSubject.Tags.Should().NotBeNull();
            testSubject.EducationPrinciples.Should().NotBeNull();
        
            testSubject.Tags.Should().HaveCount(0);
            testSubject.EducationPrinciples.Should().HaveCount(0);
        }
        
        [TestMethod]
        public void WithCleanCodeTaxonomyDisabled_SetsCctPropertiesToNull()
        {
            var languageKey = "any";
            var fullRuleKey = "any";
            var description = "any";
            var name = "any";
            var ruleIssueSeverity = RuleIssueSeverity.Critical;
            var ruleIssueType = RuleIssueType.Bug;
            var isActiveByDefault = true;
            IReadOnlyList<string> tags = new []{ "any"};
            IReadOnlyList<string> educationPrinciples = Array.Empty<string>();
            var htmlNote = "any";
            var richDescription = new RuleSplitDescriptionDto("any", new List<RuleDescriptionTabDto>());
            CleanCodeAttribute? cleanCodeAttribute = CleanCodeAttribute.Focused;
            Dictionary<SoftwareQuality,SoftwareQualitySeverity> defaultImpacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();
            
            var testSubject = new RuleInfo(
                languageKey,
                fullRuleKey,
                description,
                name,
                ruleIssueSeverity,
                ruleIssueType,
                isActiveByDefault,
                tags,
                educationPrinciples,
                htmlNote,
                richDescription,
                cleanCodeAttribute,
                defaultImpacts);
        
            var expected = new RuleInfo(
                languageKey,
                fullRuleKey,
                description,
                name,
                ruleIssueSeverity,
                ruleIssueType,
                isActiveByDefault,
                tags,
                educationPrinciples,
                htmlNote,
                richDescription,
                null,
                null);
            
            testSubject.WithCleanCodeTaxonomyDisabled().Should().BeEquivalentTo(expected);
        }
        
        [TestMethod]
        public void Ctor_SetsProperties()
        {
            var richDescription = new RuleSplitDescriptionDto("any", new List<RuleDescriptionTabDto>());
            
            var educationPrinciples = new[] { "defense_in_depth", "never_trust_user_input" };
            var defaultImpacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();
            defaultImpacts.Add(SoftwareQuality.Maintainability, SoftwareQualitySeverity.Medium);
            defaultImpacts.Add(SoftwareQuality.Reliability, SoftwareQualitySeverity.Low);
        
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
                educationPrinciples,
                "some user note",
                richDescription,
                CleanCodeAttribute.Respectful,
                defaultImpacts);
        
            testSubject.LanguageKey.Should().Be(Language.CSharp.ServerLanguage.Key);
            testSubject.FullRuleKey.Should().Be("xxx:S123");
            testSubject.Description.Should().Be("a description");
            testSubject.Name.Should().Be("the rule name");
            testSubject.Severity.Should().Be(RuleIssueSeverity.Blocker);
            testSubject.IssueType.Should().Be(RuleIssueType.Vulnerability);
            testSubject.IsActiveByDefault.Should().BeTrue();
            testSubject.Tags.Should().BeEquivalentTo(tags);
            testSubject.EducationPrinciples.Should().BeEquivalentTo(educationPrinciples);
            testSubject.HtmlNote.Should().Be("some user note");
            testSubject.RichRuleDescriptionDto.Should().BeSameAs(richDescription);
            testSubject.CleanCodeAttribute.Should().Be(CleanCodeAttribute.Respectful);
            testSubject.DefaultImpacts.Should().BeEquivalentTo(defaultImpacts);
        }
        
    }
    
    
}
