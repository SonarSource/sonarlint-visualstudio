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
                fullRuleKey: null,
                description: null,
                name: null,
                RuleIssueSeverity.Unknown,
                RuleIssueType.Unknown,
                richRuleDescriptionDto: null,
                cleanCodeAttribute: null,
                defaultImpacts: null);

            testSubject.DefaultImpacts.Should().NotBeNull();
        }
        
        
        [TestMethod]
        public void Ctor_SetsProperties()
        {
            var richDescription = new RuleSplitDescriptionDto("any", new List<RuleDescriptionTabDto>());

            var defaultImpacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();
            defaultImpacts.Add(SoftwareQuality.Maintainability, SoftwareQualitySeverity.Medium);
            defaultImpacts.Add(SoftwareQuality.Reliability, SoftwareQualitySeverity.Low);
        
            var testSubject = new RuleInfo(
                "xxx:S123",
                "a description",
                "the rule name",
                RuleIssueSeverity.Blocker,
                RuleIssueType.Vulnerability,
                richDescription,
                CleanCodeAttribute.Respectful,
                defaultImpacts);
        
            testSubject.FullRuleKey.Should().Be("xxx:S123");
            testSubject.Description.Should().Be("a description");
            testSubject.Name.Should().Be("the rule name");
            testSubject.Severity.Should().Be(RuleIssueSeverity.Blocker);
            testSubject.IssueType.Should().Be(RuleIssueType.Vulnerability);
            testSubject.RichRuleDescriptionDto.Should().BeSameAs(richDescription);
            testSubject.CleanCodeAttribute.Should().Be(CleanCodeAttribute.Respectful);
            testSubject.DefaultImpacts.Should().BeEquivalentTo(defaultImpacts);
        }
    }
}
