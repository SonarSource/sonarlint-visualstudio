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
using Moq;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client.Models;

// Based on the SonarScanner for MSBuild code
// See https://github.com/SonarSource/sonar-scanner-msbuild/blob/9ccfdb648a0411014b29c7aee8e347aeab87ea71/Tests/SonarScanner.MSBuild.PreProcessor.Tests/RuleSetGeneratorTests.cs#L29

namespace SonarLint.VisualStudio.Core.UnitTests.CSharpVB
{
    [TestClass]
    public class RuleSetGeneratorTests
    {
        private static readonly IEnumerable<SonarQubeRule> EmptyRules = Array.Empty<SonarQubeRule>();
        private static readonly IDictionary<string, string> EmptyProperties = new Dictionary<string, string>();

        private static readonly IDictionary<string, string> ValidSonarCSharpProperties = new Dictionary<string, string>
        {
            ["sonaranalyzer-cs.analyzerId"] = "SonarAnalyzer.CSharp",
            ["sonaranalyzer-cs.ruleNamespace"] = "SonarAnalyzer.CSharp",
        };

        private static readonly IList<SonarQubeRule> ValidSonarCSharpRules = new List<SonarQubeRule>
        {
            CreateSonarCSharpRule("any-1"), CreateSonarCSharpRule("any-2")
        };

        [TestMethod]
        public void Generate_ArgumentChecks()
        {
            var generator = new RuleSetGenerator();
            const string language = "cs";

            Action act1 = () => generator.Generate(null, EmptyRules, EmptyProperties);
            act1.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("language");

            Action act3 = () => generator.Generate(language, null, EmptyProperties);
            act3.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rules");

            Action act2 = () => generator.Generate(language, EmptyRules, null);
            act2.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarProperties");
        }

        [TestMethod]
        public void Generate_EmptyProperties()
        {
            // Arrange
            var generator = new RuleSetGenerator();
            var rules = new List<SonarQubeRule>
            {
                CreateRule("repo", "key"),
            };

            // Act
            var ruleSet = generator.Generate("cs", rules, EmptyProperties);

            // Assert
            ruleSet.Rules.Should().BeEmpty(); // No analyzers
            ruleSet.Description.Should().Be("This rule set was automatically generated from SonarQube");
            ruleSet.ToolsVersion.Should().Be("14.0");
            ruleSet.Name.Should().Be("Rules for SonarQube");
        }

        [TestMethod]
        public void Generate_ActiveRules_VsSeverity_IsCorrectlyMapped()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var activeRules = new List<SonarQubeRule>
            {
                CreateSonarCSharpRule("rule1", isActive: true, sqSeverity: SonarQubeIssueSeverity.Info),
                CreateSonarCSharpRule("rule2", isActive: true, sqSeverity: SonarQubeIssueSeverity.Critical)
            };

            // Act
            var ruleSet = generator.Generate("cs", activeRules, ValidSonarCSharpProperties);

            // Assert
            ruleSet.Rules.Should().HaveCount(1);
            ruleSet.Rules[0].RuleList.Select(r => r.Action).Should().BeEquivalentTo("Info", "Warning");
        }

        [TestMethod]
        public void Generate_InactiveRules_VSseverity_IsAlwaysNone()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var inactiveRules = new List<SonarQubeRule>
            {
                CreateSonarCSharpRule("rule1", isActive: false, sqSeverity: SonarQubeIssueSeverity.Major),
                CreateSonarCSharpRule("rule2", isActive: false, sqSeverity: SonarQubeIssueSeverity.Info),
            };

            // Act
            var ruleSet = generator.Generate("cs", inactiveRules, ValidSonarCSharpProperties);

            // Assert
            ruleSet.Rules.Should().HaveCount(1);
            ruleSet.Rules[0].RuleList.Select(r => r.Action).Should().BeEquivalentTo("None", "None");
        }

        [TestMethod]
        public void Generate_Unsupported_Rules_Ignored()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var rules = new[]
            {
                CreateRule("other.repo", "other.rule1", true),
                CreateRule("other.repo", "other.rule2", false),
            };

            // Act
            var ruleSet = generator.Generate("cs", rules, EmptyProperties);

            // Assert
            ruleSet.Rules.Should().BeEmpty();
        }

        [TestMethod]
        public void Generate_RoslynSDK_Rules_Added()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var properties = new Dictionary<string, string>
            {
                ["custom1.analyzerId"] = "CustomAnalyzer1",
                ["custom1.ruleNamespace"] = "CustomNamespace1",
                ["custom2.analyzerId"] = "CustomAnalyzer2",
                ["custom2.ruleNamespace"] = "CustomNamespace2",
            };

            var rules = new[]
            {
                CreateRule("roslyn.custom1", "active1", true, SonarQubeIssueSeverity.Info),
                CreateRule("roslyn.custom2", "active2", true, SonarQubeIssueSeverity.Critical),
                CreateRule("roslyn.custom1", "inactive1", false),
                CreateRule("roslyn.custom2", "inactive2", false),
            };

            // Act
            var ruleSet = generator.Generate("cs", rules, properties);

            // Assert
            ruleSet.Rules.Should().HaveCount(2);

            ruleSet.Rules[0].RuleNamespace.Should().Be("CustomNamespace1");
            ruleSet.Rules[0].AnalyzerId.Should().Be("CustomAnalyzer1");
            ruleSet.Rules[0].RuleList.Should().HaveCount(2);
            ruleSet.Rules[0].RuleList.Select(r => r.Id).Should().BeEquivalentTo("active1", "inactive1");
            ruleSet.Rules[0].RuleList.Select(r => r.Action).Should().BeEquivalentTo("Info", "None");

            ruleSet.Rules[1].RuleNamespace.Should().Be("CustomNamespace2");
            ruleSet.Rules[1].AnalyzerId.Should().Be("CustomAnalyzer2");
            ruleSet.Rules[1].RuleList.Should().HaveCount(2);
            ruleSet.Rules[1].RuleList.Select(r => r.Id).Should().BeEquivalentTo("active2", "inactive2");
            ruleSet.Rules[1].RuleList.Select(r => r.Action).Should().BeEquivalentTo("Warning", "None");
        }

        [TestMethod]
        public void Generate_Sonar_Rules_Added()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var rules = new[]
            {
                CreateRule("csharpsquid", "active1", true, SonarQubeIssueSeverity.Major),
                // Even though this rule is for VB it will be added as C#, see NOTE below
                CreateRule("vbnet", "active2", true, SonarQubeIssueSeverity.Major),

                CreateRule("csharpsquid", "inactive1", false),
                // Even though this rule is for VB it will be added as C#, see NOTE below
                CreateRule("vbnet", "inactive2", false),
            };

            // Act
            var ruleSet = generator.Generate("cs", rules, ValidSonarCSharpProperties);

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
        public void Generate_Rules_AreGroupAndSorted()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var properties = new Dictionary<string, string>
            {
                // The rules should be grouped by the analyzer id
                { "sonaranalyzer-cs.analyzerId", "SonarAnalyzer.CSharp" },
                { "wintellect.analyzerId", "AAA" },
                { "myanalyzer.analyzerId", "ZZZ" },

                // The namespace properties are required but shouldn't be used for sorting
                { "sonaranalyzer-cs.ruleNamespace", "SonarAnalyzer.CSharp" },
                { "wintellect.ruleNamespace", "XXX" },
                { "myanalyzer.ruleNamespace", "BBB" },
            };

            var rules = new[]
            {
                CreateRule("roslyn.myanalyzer", "my 1", true),

                CreateRule("roslyn.wintellect", "win2", true),
                CreateRule("roslyn.wintellect", "win1", true),
                CreateRule("roslyn.wintellect", "win0", false),

                CreateRule("csharpsquid", "S999", true),
                CreateRule("csharpsquid", "S111", false),
            };

            // Act
            var ruleSet = generator.Generate("cs", rules, properties);

            // Assert
            ruleSet.Rules.Should().HaveCount(3);

            // Expecting groups to be sorted alphabetically by analyzer id (not namespace)...
            ruleSet.Rules[0].AnalyzerId.Should().Be("AAA");
            ruleSet.Rules[1].AnalyzerId.Should().Be("SonarAnalyzer.CSharp");
            ruleSet.Rules[2].AnalyzerId.Should().Be("ZZZ");

            // ... and rules in groups to be sorted by rule key
            ruleSet.Rules[0].RuleList[0].Id.Should().Be("win0");
            ruleSet.Rules[0].RuleList[1].Id.Should().Be("win1");
            ruleSet.Rules[0].RuleList[2].Id.Should().Be("win2");

            ruleSet.Rules[1].RuleList[0].Id.Should().Be("S111");
            ruleSet.Rules[1].RuleList[1].Id.Should().Be("S999");

            ruleSet.Rules[2].RuleList[0].Id.Should().Be("my 1");
        }

        [TestMethod]
        public void Generate_Common_Parameters()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            // Act
            var ruleSet = generator.Generate("cs", EmptyRules, EmptyProperties);

            // Assert
            ruleSet.Description.Should().Be("This rule set was automatically generated from SonarQube");
            ruleSet.ToolsVersion.Should().Be("14.0");
            ruleSet.Name.Should().Be("Rules for SonarQube");
        }

        [TestMethod]
        public void Generate_AnalyzerId_Property_Missing()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var properties = new Dictionary<string, string>
            {
                { "sonaranalyzer-cs.ruleNamespace", "SonarAnalyzer.CSharp" },
            };

            // Act & Assert
            var action = new Action(() => generator.Generate("cs", ValidSonarCSharpRules, properties));
            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().StartWith(
                    "Property does not exist: sonaranalyzer-cs.analyzerId. This property should be set by the plugin in SonarQube.");
        }

        [TestMethod]
        public void Generate_RuleNamespace_Property_Missing()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var properties = new Dictionary<string, string>
            {
                { "sonaranalyzer-cs.analyzerId", "SonarAnalyzer.CSharp" },
            };

            // Act & Assert
            var action = new Action(() => generator.Generate("cs", ValidSonarCSharpRules, properties));
            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().StartWith(
                    "Property does not exist: sonaranalyzer-cs.ruleNamespace. This property should be set by the plugin in SonarQube.");
        }

        [TestMethod]
        public void Generate_PropertyName_IsCaseSensitive()
        {
            // Arrange
            var generator = new RuleSetGenerator();

            var properties = new Dictionary<string, string>
            {
                { "sonaranalyzer-cs.ANALYZERId", "SonarAnalyzer.CSharp" },
                { "sonaranalyzer-cs.ruleNamespace", "SonarAnalyzer.CSharp" },
            };

            // Act & Assert
            var action = new Action(() => generator.Generate("cs", ValidSonarCSharpRules, properties));
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
            Action act = () => RuleSetGenerator.GetActionText((RuleAction)(-1));
            act.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        [DataRow(SonarQubeIssueSeverity.Info, RuleAction.Info)]
        [DataRow(SonarQubeIssueSeverity.Minor, RuleAction.Info)]
        [DataRow(SonarQubeIssueSeverity.Major, RuleAction.Warning)]
        [DataRow(SonarQubeIssueSeverity.Critical, RuleAction.Warning)]
        public void GetVSSeverity_NotBlocker_CorrectlyMapped(SonarQubeIssueSeverity sqSeverity, RuleAction expectedVsSeverity)
        {
            var testSubject = new RuleSetGenerator();

            testSubject.GetVsSeverity(sqSeverity).Should().Be(expectedVsSeverity);
        }

        [TestMethod]
        [DataRow(true, RuleAction.Error)]
        [DataRow(false, RuleAction.Warning)]
        public void GetVSSeverity_Blocker_CorrectlyMapped(bool shouldTreatBlockerAsError, RuleAction expectedVsSeverity)
        {
            var envSettingsMock = new Mock<IEnvironmentSettings>();
            envSettingsMock.Setup(x => x.TreatBlockerSeverityAsError()).Returns(shouldTreatBlockerAsError);

            var testSubject = new RuleSetGenerator(envSettingsMock.Object);

            testSubject.GetVsSeverity(SonarQubeIssueSeverity.Blocker).Should().Be(expectedVsSeverity);
        }

        [TestMethod]
        [DataRow(SonarQubeIssueSeverity.Unknown)]
        [DataRow((SonarQubeIssueSeverity)(-1))]
        public void GetVSSeverity_Invalid_Throws(SonarQubeIssueSeverity sqSeverity)
        {
            Action act = () => new RuleSetGenerator().GetVsSeverity(sqSeverity);
            act.Should().Throw<NotSupportedException>();
        }

        private static SonarQubeRule CreateSonarCSharpRule(string ruleKey, bool isActive = true, SonarQubeIssueSeverity sqSeverity = SonarQubeIssueSeverity.Info) =>
            CreateRule("csharpsquid", ruleKey, isActive, sqSeverity);

        private static SonarQubeRule CreateRule(string repoKey, string ruleKey, bool isActive = true, SonarQubeIssueSeverity sqSeverity = SonarQubeIssueSeverity.Info) =>
            new SonarQubeRule(ruleKey, repoKey, isActive, sqSeverity, new Dictionary<string, string>());
    }
}
