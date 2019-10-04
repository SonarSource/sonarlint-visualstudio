/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.RulesLoader;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class RulesMetadataCacheTest
    {
        // Rule data for C-Family plugin v6.3 (build 11371)
        private const int Active_C_Rules = 154;
        private const int Inactive_C_Rules = 104;

        private const int Active_CPP_Rules = 245;
        private const int Inactive_CPP_Rules = 146;

        [TestMethod]
        public void Read_Rules()
        {
            RulesLoader.ReadRulesList().Should().HaveCount(410); // unexpanded list of keys

            RulesMetadataCache.GetSettings("c").AllPartialRuleKeys.Should().HaveCount(Active_C_Rules + Inactive_C_Rules);
            RulesMetadataCache.GetSettings("cpp").AllPartialRuleKeys.Should().HaveCount(Active_CPP_Rules + Inactive_CPP_Rules);

            // We don't currently support ObjC rules in VS
            RulesMetadataCache.GetSettings("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Active_Rules()
        {
            RulesLoader.ReadActiveRulesList().Should().HaveCount(255); // unexpanded list of active rules

            RulesMetadataCache.GetSettings("c").ActivePartialRuleKeys.Should().HaveCount(Active_C_Rules);
            RulesMetadataCache.GetSettings("cpp").ActivePartialRuleKeys.Should().HaveCount(Active_CPP_Rules);

            // We don't currently support ObjC rules in VS
            RulesMetadataCache.GetSettings("objc").Should().BeNull();
        }        

        [TestMethod]
        public void Read_Rules_Params()
        {
            IDictionary<string, string> parameters = null;
            RulesMetadataCache.GetSettings("cpp").RulesParameters.TryGetValue("ClassComplexity", out parameters);
            parameters.Should()
                .Contain(new System.Collections.Generic.KeyValuePair<string, string>("maximumClassComplexityThreshold", "80"));

        }

        [TestMethod]
        public void Read_Rules_Metadata()
        {
            RuleMetadata metadata = null;
            RulesMetadataCache.GetSettings("cpp").RulesMetadata.TryGetValue("ClassComplexity", out metadata);
            using (new AssertionScope())
            {
                metadata.Type.Should().Be(Sonarlint.Issue.Types.Type.CodeSmell);
                metadata.DefaultSeverity.Should().Be(Sonarlint.Issue.Types.Severity.Critical);
            }
        }
    }
}
