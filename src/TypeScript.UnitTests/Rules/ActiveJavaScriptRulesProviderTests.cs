/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Rules
{
    [TestClass]
    public class ActiveJavaScriptRulesProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ActiveJavaScriptRulesProvider, IActiveJavaScriptRulesProvider>(null, new[]
            {
                MefTestHelpers.CreateExport<IJavaScriptRuleDefinitionsProvider>(Mock.Of<IJavaScriptRuleDefinitionsProvider>())
            });
        }

        [TestMethod]
        public void Get_InactiveRulesAreNotReturned()
        {
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule("active 1", activeByDefault: true);
            ruleDefns.AddRule("inactive AAA", activeByDefault: false);
            ruleDefns.AddRule("active 2", activeByDefault: true);
            ruleDefns.AddRule("inactive BBB", activeByDefault: false);

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns);

            var result = testSubject.Get().ToArray();

            CheckExpectedRuleKeys(result, "active 1", "active 2");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_HotspotsAreNotReturned()
        {
            // NOTE: there are currently no taint vulnerabilities in the SonarJS jar
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule("bug", ruleType: RuleType.BUG);
            ruleDefns.AddRule("codesmell", ruleType: RuleType.CODE_SMELL);
            ruleDefns.AddRule("hotspot", ruleType: RuleType.SECURITY_HOTSPOT);
            ruleDefns.AddRule("vuln", ruleType: RuleType.VULNERABILITY);

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns);

            var result = testSubject.Get().ToArray();

            CheckExpectedRuleKeys(result, "bug", "codesmell", "vuln");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_RulesWithNullEslintKeysAreNotReturned()
        {
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule("aaa");
            ruleDefns.AddRule(null);
            ruleDefns.AddRule("bbb");

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns);

            var result = testSubject.Get().ToArray();

            CheckExpectedRuleKeys(result, "aaa", "bbb");
            CheckConfigurationsAreEmpty(result);
        }

        private static void CheckExpectedRuleKeys(IEnumerable<Rule> result, params string[] expected) =>
            result.Select(x => x.Key).Should().BeEquivalentTo(expected);

        private static void CheckConfigurationsAreEmpty(IEnumerable<Rule> result) =>
            result.All(x => x.Configurations.Length == 0).Should().BeTrue();

        private class RuleDefinitionsBuilder : IJavaScriptRuleDefinitionsProvider
        {
            private readonly IList<RuleDefinition> definitions = new List<RuleDefinition>();

            IEnumerable<RuleDefinition> IJavaScriptRuleDefinitionsProvider.GetDefinitions() => definitions;

            public void AddRule(string eslintKey, bool activeByDefault = true, RuleType ruleType = RuleType.BUG)
            {
                var newDefn = new RuleDefinition
                {
                    EslintKey = eslintKey,
                    ActivatedByDefault = activeByDefault,
                    Type = ruleType
                };

                definitions.Add(newDefn);
            }
        }
    }
}
