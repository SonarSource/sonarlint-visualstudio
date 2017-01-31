/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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
