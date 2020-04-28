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

namespace SonarLint.VisualStudio.Core.UnitTests.CSharpVB
{
    [TestClass]
    public class SonarLintConfigGeneratorTests
    {
        private static readonly IEnumerable<SonarQubeRule> EmptyRules = Array.Empty<SonarQubeRule>();
        private static readonly IDictionary<string, string> EmptyProperties = new Dictionary<string, string>();
        private const string ValidLanguage = "cs";

        [TestMethod]
        public void Generate_NullArguments_Throws()
        {
            Action act = () => SonarLintConfigGenerator.Generate(null, EmptyProperties, ValidLanguage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rules");

            act = () => SonarLintConfigGenerator.Generate(EmptyRules, null, ValidLanguage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarProperties");

            act = () => SonarLintConfigGenerator.Generate(EmptyRules, EmptyProperties, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("xxx")]
        [DataRow("CS")] // should be case-sensitive
        [DataRow("vb")] // VB language key is "vbnet"
        public void Generate_UnrecognisedLanguage_Throws(string language)
        {
            Action act = () => SonarLintConfigGenerator.Generate(EmptyRules, EmptyProperties, language);
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        [DataRow("cs")]
        [DataRow("vbnet")]
        public void Generate_NoActiveRulesOrSettings_ValidLanguage_ReturnsValidConfig(string validLanguage)
        {
            var actual = SonarLintConfigGenerator.Generate(EmptyRules, EmptyProperties, validLanguage);

            actual.Should().NotBeNull();
            actual.Rules.Should().BeEmpty();
            actual.Settings.Should().BeEmpty();
        }

        [TestMethod]
        public void Generate_ValidSettings_OnlyLanguageSpecificSettingsReturned()
        {
            // Arrange
            var properties = new Dictionary<string, string>
            {
                { "sonar.cs.property1", "valid setting 1"},
                { "sonar.cs.property2", "valid setting 2"},
                { "sonar.vbnet.property1", "wrong language - not returned"},
                { "sonar.CS.property2", "wrong case - not returned"},
                { "sonar.cs.", "incorrect prefix - not returned"},
                { "xxx.cs.property1", "key does not match - not returned"},
                { ".does.not.match", "not returned"}
            };

            // Act
            var actual = SonarLintConfigGenerator.Generate(EmptyRules, properties, "cs");

            // Assert
            actual.Settings.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                { "sonar.cs.property1", "valid setting 1"},
                { "sonar.cs.property2", "valid setting 2"}
            });
        }

        [TestMethod]
        public void Generate_ValidSettings_SecuredSettingsAreNotReturned()
        {
            // Arrange
            var properties = new Dictionary<string, string>
            {
                { "sonar.cs.property1.secured", "secure - should not be returned"},
                { "sonar.cs.property2", "valid setting"},
                { "sonar.cs.property3.SECURED", "secure - should not be returned2"},
            };

            // Act
            var actual = SonarLintConfigGenerator.Generate(EmptyRules, properties, "cs");

            // Assert
            actual.Settings.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                { "sonar.cs.property1", "valid setting"}
            });
        }

        [TestMethod]
        [DataRow("cs", "csharpsquid")]
        [DataRow("vbnet", "vbnet")]
        public void Generate_ValidRules_OnlyRulesFromKnownRepositoryReturned(string knownLanguage, string knownRepoKey)
        {
            var rules = new List<SonarQubeRule>()
            {
                CreateRule("valid1", knownRepoKey),
                CreateRule("unknown1", "unknown.repo.key"),
                CreateRule("valid2", knownRepoKey),
                CreateRule("invalid2", "another.unknown.repo.key"),
                CreateRule("valid3", knownRepoKey)
            };

            // Act
            var actual = SonarLintConfigGenerator.Generate(rules, EmptyProperties, knownLanguage);

            // Assert
            actual.Rules.Select(r => r.Key).Should().BeEquivalentTo(new string[] { "valid1", "valid2", "valid3" });
        }

        [TestMethod]
        public void Generate_RulesWithParameters_ExpectedConfigReturned()
        {
            var rule1Params = new Dictionary<string, string> { { "param1", "value1" }, { "param2", "value2" } };
            var rule2Params = new Dictionary<string, string> { { "param3", "value4" } };

            var rules = new List<SonarQubeRule>()
            {
                CreateRule("s111", "csharpsquid", rule1Params ),
                CreateRule("s222", "csharpsquid", rule2Params ),
            };

            // Act
            var actual = SonarLintConfigGenerator.Generate(rules, EmptyProperties, "cs");

            // Assert
            actual.Rules.Count.Should().Be(2);

            actual.Rules[0].Key.Should().Be("s111");
            actual.Rules[0].Parameters.Should().BeEquivalentTo(rule1Params);
            actual.Rules[1].Parameters.Should().BeEquivalentTo(rule2Params);
        }

        [TestMethod]
        public void Generate_Serialized_ReturnsExpectedXml()
        {
            var properties = new Dictionary<string, string>()
            {
                { "sonar.cs.prop1", "value 1"},
                { "sonar.cs.prop2", "value 2"}
            };

            var rules = new List<SonarQubeRule>()
            {
                CreateRule("s111", "csharpsquid"),
                CreateRule("s222", "csharpsquid",
                    new Dictionary<string, string> { { "param1", "param value1" }, { "param2", "param value2" } }),
            };

            // Act
            var actual = SonarLintConfigGenerator.Generate(rules, properties, "cs");
            var actualXml = Serializer.ToString(actual);

            // Assert
            actualXml.Should().Be(@"<?xml version=""1.0"" encoding=""utf-8""?>
<AnalysisInput xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Settings>
    <Setting>
      <Key>sonar.cs.prop1</Key>
      <Value>value 1</Value>
    </Setting>
    <Setting>
      <Key>sonar.cs.prop2</Key>
      <Value>value 2</Value>
    </Setting>
  </Settings>
  <Rules>
    <Rule>
      <Key>s111</Key>
    </Rule>
    <Rule>
      <Key>s222</Key>
      <Parameters>
        <Parameter>
          <Key>param1</Key>
          <Value>param value1</Value>
        </Parameter>
        <Parameter>
          <Key>param2</Key>
          <Value>param value2</Value>
        </Parameter>
      </Parameters>
    </Rule>
  </Rules>
</AnalysisInput>");
        }

        private static SonarQubeRule CreateRule(string ruleKey, string repoKey, IDictionary<string, string> parameters = null) =>
            new SonarQubeRule(ruleKey, repoKey, isActive: false, SonarQubeIssueSeverity.Blocker, parameters);
    }
}
