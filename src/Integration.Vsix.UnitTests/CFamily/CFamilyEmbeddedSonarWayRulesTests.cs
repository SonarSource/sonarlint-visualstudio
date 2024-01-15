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

using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CFamilyEmbeddedSonarWayRulesTests
    {
        // Sanity checks that the rules metata for the CFamily plugin is present and can be loaded

        // Note: how to find the expected number of active/inactive rules in SonarWay by language:
        // 1. Start a local SQ instance with the correct plugin version installed
        // 2. Browse to "Rules" e.g. http://localhost:9000/
        //    or:
        //    SonarCloud: C: https://sonarcloud.io/organizations/sonarsource/quality_profiles/show?name=Sonar+way&language=c
        //    SonarCloud: C++: https://sonarcloud.io/organizations/sonarsource/quality_profiles/show?name=Sonar+way&language=cpp
        // note: if you just look at the qp page, then it always shows 6 more inactive rules for c&cpp than available in the ide
        // 3. Filter by Repository = SonarAnalyzer C
        // 4. Filter by Quality Profile = Sonar way C
        // The QP filter has "active/inactive" tabs. The number of rules is shown in the top-right of the screen.
        // 5. Repeat for C++.

        // You can check the version of the plugin that is installed on the appropriate web API:
        // e.g. https://next.sonarqube.com/sonarqube/api/plugins/installed and https://sonarcloud.io/api/plugins/installed
        // Note - you need to be logged in.

        // Rule data for C-Family plugin v6.49.0.62722

        private const int Active_C_Rules = 210;
        private const int Inactive_C_Rules = 124;

        private const int Active_CPP_Rules = 436;
        private const int Inactive_CPP_Rules = 209;

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
            // The choice of rule ID here is arbitrary - any rule that has parameters will do.
            rulesMetadataCache.GetRulesConfiguration("cpp").RulesParameters.TryGetValue("S100", out var parameters);
            parameters.Should()
                .Contain(new System.Collections.Generic.KeyValuePair<string, string>("format", "^[a-z][a-zA-Z0-9]*$"));
        }

        [TestMethod]
        public void Read_Rules_Metadata()
        {
            // The choice of rule ID here is arbitrary - any rule will do
            rulesMetadataCache.GetRulesConfiguration("cpp").RulesMetadata.TryGetValue("S100", out var metadata);
            using (new AssertionScope())
            {
                metadata.Type.Should().Be(IssueType.CodeSmell);
                metadata.DefaultSeverity.Should().Be(IssueSeverity.Minor);
            }
        }

        [TestMethod]
        [DataRow("S5536", "c")]
        [DataRow("S5536", "cpp")]
        public void CheckProjectLevelRule_IsDisabledByDefault(string ruleKey, string languageKey)
        {
            // The choice of rule ID here is arbitrary - any rule will do
            rulesMetadataCache.GetRulesConfiguration(languageKey).AllPartialRuleKeys.Contains(ruleKey).Should().BeTrue();
            rulesMetadataCache.GetRulesConfiguration(languageKey).ActivePartialRuleKeys.Contains(ruleKey).Should().BeFalse();
        }
    }
}
