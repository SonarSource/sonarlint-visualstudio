//-----------------------------------------------------------------------
// <copyright file="TestRuleSetHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System.Collections.Generic;
using System.IO;

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
