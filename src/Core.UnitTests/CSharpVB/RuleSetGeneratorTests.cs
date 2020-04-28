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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client.Models;

// Copied from the SonarScanner for MSBuild.
// See https://github.com/SonarSource/sonar-scanner-msbuild/blob/9ccfdb648a0411014b29c7aee8e347aeab87ea71/Tests/SonarScanner.MSBuild.PreProcessor.Tests/RuleSetGeneratorTests.cs#L29

namespace SonarLint.VisualStudio.Core.UnitTests.CSharpVB
{
    [TestClass]
    public class RuleSetGeneratorTests
    {
        [TestMethod]
        public void RoslynRuleSet_ConstructorArgumentChecks()
        {
            Action act = () => new RuleSetGenerator(null);
            act.Should().ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void RoslynRuleSet_GeneratorArgumentChecks()
        {
            var generator = new RuleSetGenerator(new Dictionary<string, string>());
            IEnumerable<SonarQubeRule> activeRules = new List<SonarQubeRule>();
            IEnumerable<SonarQubeRule> inactiveRules = new List<SonarQubeRule>();
            var language = "cs";

            Action act1 = () => generator.Generate(null, activeRules, inactiveRules);
            act1.Should().ThrowExactly<ArgumentNullException>();

            Action act2 = () => generator.Generate(language, activeRules, null);
            act2.Should().ThrowExactly<ArgumentNullException>();

            Action act3 = () => generator.Generate(language, null, inactiveRules);
            act3.Should().ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void RoslynRuleSet_Empty()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>());
            var activeRules = new List<SonarQubeRule>
            {
                CreateRule("repo", "key", false),
            };
            var inactiveRules = new List<SonarQubeRule>();

            // Act
            var ruleSet = generator.Generate("cs", activeRules, inactiveRules);

            // Assert
            ruleSet.Rules.Should().BeEmpty(); // No analyzers
            ruleSet.Description.Should().Be("This rule set was automatically generated from SonarQube");
            ruleSet.ToolsVersion.Should().Be("14.0");
            ruleSet.Name.Should().Be("Rules for SonarQube");
        }

        [TestMethod]
        public void RoslynRuleSet_ActiveRules_Always_Warning()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>
            {
                ["sonaranalyzer-cs.analyzerId"] = "SonarAnalyzer.CSharp",
                ["sonaranalyzer-cs.ruleNamespace"] = "SonarAnalyzer.CSharp",
            });

            var activeRules = new List<SonarQubeRule>
            {
                CreateRule("csharpsquid", "rule1", isActive: true),
                CreateRule("csharpsquid", "rule2", isActive: true),
            };

            // Act
            var ruleSet = generator.Generate("cs", activeRules, Enumerable.Empty<SonarQubeRule>());

            // Assert
            ruleSet.Rules.Should().HaveCount(1);
            ruleSet.Rules[0].RuleList.Select(r => r.Action).Should().BeEquivalentTo("Warning", "Warning");
        }

        [TestMethod]
        public void RoslynRuleSet_InactiveRules_Always_None()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>
            {
                ["sonaranalyzer-cs.analyzerId"] = "SonarAnalyzer.CSharp",
                ["sonaranalyzer-cs.ruleNamespace"] = "SonarAnalyzer.CSharp",
            });

            var inactiveRules = new List<SonarQubeRule>
            {
                CreateRule("csharpsquid", "rule1", isActive: false),
                CreateRule("csharpsquid", "rule2", isActive: false),
            };

            // Act
            var ruleSet = generator.Generate("cs", Enumerable.Empty<SonarQubeRule>(), inactiveRules);

            // Assert
            ruleSet.Rules.Should().HaveCount(1);
            ruleSet.Rules[0].RuleList.Select(r => r.Action).Should().BeEquivalentTo("None", "None");
        }

        [TestMethod]
        public void RoslynRuleSet_Unsupported_Rules_Ignored()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>());

            var activeRules = new[]
            {
                CreateRule("other.repo", "other.rule1", true),
            };
            var inactiveRules = new[]
            {
                CreateRule("other.repo", "other.rule2", false),
            };

            // Act
            var ruleSet = generator.Generate("cs", activeRules, inactiveRules);

            // Assert
            ruleSet.Rules.Should().BeEmpty();
        }

        [TestMethod]
        public void RoslynRuleSet_RoslynSDK_Rules_Added()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>
            {
                ["custom1.analyzerId"] = "CustomAnalyzer1",
                ["custom1.ruleNamespace"] = "CustomNamespace1",
                ["custom2.analyzerId"] = "CustomAnalyzer2",
                ["custom2.ruleNamespace"] = "CustomNamespace2",
            });

            var activeRules = new[]
            {
                CreateRule("roslyn.custom1", "active1", true),
                CreateRule("roslyn.custom2", "active2", true),
            };
            var inactiveRules = new[]
            {
                CreateRule("roslyn.custom1", "inactive1", false),
                CreateRule("roslyn.custom2", "inactive2", false),
            };

            // Act
            var ruleSet = generator.Generate("cs", activeRules, inactiveRules);

            // Assert
            ruleSet.Rules.Should().HaveCount(2);

            ruleSet.Rules[0].RuleNamespace.Should().Be("CustomNamespace1");
            ruleSet.Rules[0].AnalyzerId.Should().Be("CustomAnalyzer1");
            ruleSet.Rules[0].RuleList.Should().HaveCount(2);
            ruleSet.Rules[0].RuleList.Select(r => r.Id).Should().BeEquivalentTo("active1", "inactive1");
            ruleSet.Rules[0].RuleList.Select(r => r.Action).Should().BeEquivalentTo("Warning", "None");

            ruleSet.Rules[1].RuleNamespace.Should().Be("CustomNamespace2");
            ruleSet.Rules[1].AnalyzerId.Should().Be("CustomAnalyzer2");
            ruleSet.Rules[1].RuleList.Should().HaveCount(2);
            ruleSet.Rules[1].RuleList.Select(r => r.Id).Should().BeEquivalentTo("active2", "inactive2");
            ruleSet.Rules[1].RuleList.Select(r => r.Action).Should().BeEquivalentTo("Warning", "None");
        }

        [TestMethod]
        public void RoslynRuleSet_Sonar_Rules_Added()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>
            {
                { "sonaranalyzer-cs.analyzerId", "SonarAnalyzer.CSharp" },
                { "sonaranalyzer-cs.ruleNamespace", "SonarAnalyzer.CSharp" },
            });

            var activeRules = new[]
            {
                CreateRule("csharpsquid", "active1", true),
                // Even though this rule is for VB it will be added as C#, see NOTE below
                CreateRule("vbnet", "active2", true),
            };
            var inactiveRules = new[]
            {
                CreateRule("csharpsquid", "inactive1", false),
                // Even though this rule is for VB it will be added as C#, see NOTE below
                CreateRule("vbnet", "inactive2", false),
            };

            // Act
            var ruleSet = generator.Generate("cs", activeRules, inactiveRules);

            // Assert
            ruleSet.Rules.Should().HaveCount(1);

            // NOTE: The RuleNamespace and AnalyzerId are taken from the language parameter of the
            // Generate method. The FetchArgumentsAndRulesets method will retrieve active/inactive
            // rules from SonarQube per language/quality profile and mixture of VB-C# rules is not
            // expected.
            ruleSet.Rules[0].RuleNamespace.Should().Be("SonarAnalyzer.CSharp");
            ruleSet.Rules[0].AnalyzerId.Should().Be("SonarAnalyzer.CSharp");
            ruleSet.Rules[0].RuleList.Should().HaveCount(4);
            ruleSet.Rules[0].RuleList.Select(r => r.Id).Should().BeEquivalentTo(
                "active1", "active2", "inactive1", "inactive2");
            ruleSet.Rules[0].RuleList.Select(r => r.Action).Should().BeEquivalentTo(
                "Warning", "Warning", "None", "None");
        }

        [TestMethod]
        public void RoslynRuleSet_Common_Parameters()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>());

            // Act
            var ruleSet = generator.Generate("cs", Enumerable.Empty<SonarQubeRule>(), Enumerable.Empty<SonarQubeRule>());

            // Assert
            ruleSet.Description.Should().Be("This rule set was automatically generated from SonarQube");
            ruleSet.ToolsVersion.Should().Be("14.0");
            ruleSet.Name.Should().Be("Rules for SonarQube");
        }

        [TestMethod]
        public void RoslynRuleSet_AnalyzerId_Property_Missing()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>
            {
                { "sonaranalyzer-cs.ruleNamespace", "SonarAnalyzer.CSharp" },
            });

            var activeRules = new[]
            {
                CreateRule("csharpsquid", "active1", true),
            };

            // Act & Assert
            var action = new Action(() => generator.Generate("cs", activeRules, Enumerable.Empty<SonarQubeRule>()));
            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().StartWith(
                    "Property does not exist: sonaranalyzer-cs.analyzerId. This property should be set by the plugin in SonarQube.");
        }

        [TestMethod]
        public void RoslynRuleSet_RuleNamespace_Property_Missing()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>
            {
                { "sonaranalyzer-cs.analyzerId", "SonarAnalyzer.CSharp" },
            });

            var activeRules = new[]
            {
                CreateRule("csharpsquid", "active1", true),
            };

            // Act & Assert
            var action = new Action(() => generator.Generate("cs", activeRules, Enumerable.Empty<SonarQubeRule>()));
            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().StartWith(
                    "Property does not exist: sonaranalyzer-cs.ruleNamespace. This property should be set by the plugin in SonarQube.");
        }

        [TestMethod]
        public void RoslynRuleSet_PropertyName_IsCaseSensitive()
        {
            // Arrange
            var generator = new RuleSetGenerator(new Dictionary<string, string>
            {
                { "sonaranalyzer-cs.ANALYZERId", "SonarAnalyzer.CSharp" },
                { "sonaranalyzer-cs.ruleNamespace", "SonarAnalyzer.CSharp" },
            });

            var activeRules = new[]
            {
                CreateRule("csharpsquid", "active1", true),
            };

            // Act & Assert
            var action = new Action(() => generator.Generate("cs", activeRules, Enumerable.Empty<SonarQubeRule>()));
            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().StartWith(
                    "Property does not exist: sonaranalyzer-cs.analyzerId. This property should be set by the plugin in SonarQube.");
        }


        [TestMethod]
        [DataRow(RuleAction.Info, "Info")]
        [DataRow(RuleAction.Warning, "Warning")]
        [DataRow(RuleAction.None, "None")]
        [DataRow(RuleAction.Error, "Error")]
        [DataRow(RuleAction.Hidden, "Hidden")]
        public void GetActionText_Valid(RuleAction action, string expected)
        {
            RuleSetGenerator.GetActionText(action).Should().Be(expected);
        }

        [TestMethod]
        public void GetActionText_Invalid()
        {
            Action act = () =>RuleSetGenerator.GetActionText((RuleAction)(-1));
            act.Should().Throw<NotSupportedException>();
        }

        private SonarQubeRule CreateRule(string repoKey, string ruleKey, bool isActive) =>
            new SonarQubeRule(ruleKey, repoKey, isActive, SonarQubeIssueSeverity.Unknown, new Dictionary<string, string>());
    }
}
