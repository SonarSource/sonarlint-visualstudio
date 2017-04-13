/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.IO;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal static class TestRuleSetHelper
    {
        public static RuleSet CreateTestRuleSet(string fullPath)
        {
            return new RuleSet(Constants.RuleSetName) { FilePath = fullPath };
        }

        public static RuleSet CreateTestRuleSet(string rootDir, string fileName)
        {
            return CreateTestRuleSet(Path.Combine(rootDir, fileName));
        }

        public static RuleSet CreateTestRuleSet(int numRules, IEnumerable<string> includes = null)
        {
            var ruleSet = new RuleSet(Constants.RuleSetName);
            for (int i = 0; i < numRules; i++)
            {
                ruleSet.Rules.Add(new RuleReference("MyAnalzerId", "MyNamespace", "AWESOME" + i, RuleAction.Warning));
            }

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    ruleSet.RuleSetIncludes.Add(new RuleSetInclude(include, RuleAction.Default));
                }
            }

            return ruleSet;
        }

        public static RuleSet CreateTestRuleSetWithRuleIds(IEnumerable<string> ids, string analyzerId = "TestId", string ruleNamespace = "TestNamespace", RuleSet existingRuleSet = null)
        {
            var ruleSet = existingRuleSet ?? new RuleSet("Test Rule Set");
            foreach (var id in ids)
            {
                ruleSet.Rules.Add(new RuleReference(analyzerId, ruleNamespace, id, RuleAction.Warning));
            }
            return ruleSet;
        }

        public static RuleSet XmlToRuleSet(string xml)
        {
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, xml);
            return RuleSet.LoadFromFile(tempFilePath);
        }

        public static string RuleSetToXml(RuleSet ruleSet)
        {
            string tempFilePath = Path.GetTempFileName();
            ruleSet.WriteToFile(tempFilePath);
            return File.ReadAllText(tempFilePath);
        }
    }
}