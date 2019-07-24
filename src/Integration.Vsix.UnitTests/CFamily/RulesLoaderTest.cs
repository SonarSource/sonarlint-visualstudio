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

using System;
using System.IO;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.RulesLoader;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class RulesLoaderTest
    {
        [TestMethod]
        public void Read_Rules()
        {
            RulesLoader.ReadRulesList().Should().HaveCount(410);
        }

        [TestMethod]
        public void Read_Active_Rules()
        {
            RulesLoader.ReadActiveRulesList().Should().HaveCount(255);
        }

        [TestMethod]
        public void Read_Rules_Params()
        {
            RulesLoader.ReadRuleParams("ClassComplexity").Should()
                .Contain(new System.Collections.Generic.KeyValuePair<string, string>("maximumClassComplexityThreshold", "80"));

            RulesLoader.ReadRuleParams("Missing").Should().BeEmpty();

            // Sanity check, ensure we can read all rules params
            foreach (string ruleKey in RulesLoader.ReadRulesList())
            {
                RulesLoader.ReadRuleParams(ruleKey).Should().NotBeNull();
            }
        }

        [TestMethod]
        public void Read_Rules_Metadata()
        {
            using (new AssertionScope())
            {
                RulesLoader.ReadRuleMetadata("ClassComplexity").Type.Should().Be(RuleType.CodeSmell);
                RulesLoader.ReadRuleMetadata("ClassComplexity").DefaultSeverity.Should().Be(RuleSeverity.Critical);
            }

            Action act = () => RulesLoader.ReadRuleMetadata("Missing");
            act.Should().ThrowExactly<FileNotFoundException>();

            // Sanity check, ensure we can read all rules
            foreach (string ruleKey in RulesLoader.ReadRulesList())
            {
                RulesLoader.ReadRuleMetadata(ruleKey).Should().NotBeNull();
            }
        }
    }

}
