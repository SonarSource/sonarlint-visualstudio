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

using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CFamilyEmbeddedSonarWayRulesTests
    {
        // Sanity checks that the rules metata for the CFamily plugin is present and can be loaded

        // Note: how to find the expected number of active/inactive rules in SonarWay by language:
        // 1. Start a local SQ instance with the correct plugin version installed
        // 2. Browse to "Rules" e.g. http://localhost:9000/
        // 3. Filter by Repository = SonarAnalyzer C
        // 4. Filter by Quality Profile = Sonar way C
        // The QP filter has "active/inactive" tabs. The number of rules is shown in the top-right of the screen.
        // 5. Repeat for C++.

        // Rule data for C-Family plugin v6.12 (build 20255)
        private const int Active_C_Rules = 181;
        private const int Inactive_C_Rules = 104;

        private const int Active_CPP_Rules = 290;
        private const int Inactive_CPP_Rules = 168;

        private readonly CFamilySonarWayRulesConfigProvider rulesMetadataCache = new CFamilySonarWayRulesConfigProvider(CFamilyShared.CFamilyFilesDirectory);

        [TestMethod]
        public void Read_Rules()
        {
            rulesMetadataCache.GetRulesConfiguration("c").AllPartialRuleKeys.Should().HaveCount(Active_C_Rules + Inactive_C_Rules);
            rulesMetadataCache.GetRulesConfiguration("cpp").AllPartialRuleKeys.Should().HaveCount(Active_CPP_Rules + Inactive_CPP_Rules);

            // We don't currently support ObjC rules in VS
            rulesMetadataCache.GetRulesConfiguration("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Active_Rules()
        {
            rulesMetadataCache.GetRulesConfiguration("c").ActivePartialRuleKeys.Should().HaveCount(Active_C_Rules);
            rulesMetadataCache.GetRulesConfiguration("cpp").ActivePartialRuleKeys.Should().HaveCount(Active_CPP_Rules);

            // We don't currently support ObjC rules in VS
            rulesMetadataCache.GetRulesConfiguration("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Rules_Params()
        {
            rulesMetadataCache.GetRulesConfiguration("cpp").RulesParameters.TryGetValue("ClassComplexity", out var parameters);
            parameters.Should()
                .Contain(new System.Collections.Generic.KeyValuePair<string, string>("maximumClassComplexityThreshold", "80"));
        }

        [TestMethod]
        public void Read_Rules_Metadata()
        {
            rulesMetadataCache.GetRulesConfiguration("cpp").RulesMetadata.TryGetValue("ClassComplexity", out var metadata);
            using (new AssertionScope())
            {
                metadata.Type.Should().Be(IssueType.CodeSmell);
                metadata.DefaultSeverity.Should().Be(IssueSeverity.Critical);
            }
        }

        [TestMethod]
        [DataRow("S5536", "c")]
        [DataRow("S5536", "cpp")]
        public void CheckProjectLevelRule_IsDisabledByDefault(string ruleKey, string languageKey)
        {
            rulesMetadataCache.GetRulesConfiguration(languageKey).AllPartialRuleKeys.Contains(ruleKey).Should().BeTrue();
            rulesMetadataCache.GetRulesConfiguration(languageKey).ActivePartialRuleKeys.Contains(ruleKey).Should().BeFalse();
        }
    }
}
