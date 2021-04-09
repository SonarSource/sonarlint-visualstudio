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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
{
    [TestClass]
    public class EslintBridgeIssueConverterTests
    {
        private const string TestEslintBridgeRuleId = "rule id";

        [TestMethod]
        public void Convert_IssueConverted()
        {
            var eslintBridgeIssue = new Issue
            {
                RuleId = "rule id",
                Column = 1,
                EndColumn = 2,
                Line = 3,
                EndLine = 4,
                Message = "some message"
            };

            ConvertToSonarRuleKey keyMapper = inputKey => "mapped " + inputKey;
            GetRuleDefinitions ruleDefinitionsProvider = () => new[]
            {
                new RuleDefinition
                {
                    EslintKey = eslintBridgeIssue.RuleId,
                    Type = RuleType.CODE_SMELL,
                    Severity = RuleSeverity.MAJOR
                }
            };

            var testSubject = CreateTestSubject(keyMapper, ruleDefinitionsProvider);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.RuleKey.Should().Be("mapped rule id");
            convertedIssue.StartLine.Should().Be(3);
            convertedIssue.StartLineOffset.Should().Be(1);
            convertedIssue.EndLine.Should().Be(4);
            convertedIssue.EndLineOffset.Should().Be(2);
            convertedIssue.Message.Should().Be("some message");
            convertedIssue.LineHash.Should().BeNull();
            convertedIssue.Type.Should().Be(AnalysisIssueType.CodeSmell);
            convertedIssue.Severity.Should().Be(AnalysisIssueSeverity.Major);
        }

        [TestMethod]
        public void Convert_NoSecondaryLocations_IssueWithoutFlows()
        {
            var eslintBridgeIssue = new Issue
            {
                RuleId = TestEslintBridgeRuleId,
                Column = 1,
                EndColumn = 2,
                Line = 3,
                EndLine = 4,
                Message = "some message",
                SecondaryLocations = null
            };

            var testSubject = CreateTestSubject();
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_HasSecondaryLocations_IssueWithSingleFlowAndLocations()
        {
            var eslintBridgeIssue = new Issue
            {
                RuleId = TestEslintBridgeRuleId,
                SecondaryLocations = new[]
                {
                    new IssueLocation {Column = 1, EndColumn = 2, Line = 3, EndLine = 4, Message = "message1"},
                    new IssueLocation {Column = 5, EndColumn = 6, Line = 7, EndLine = 8, Message = "message2"},
                }
            };

            var testSubject = CreateTestSubject();
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("message1", "some file", 3, 4, 1, 2, null),
                new AnalysisIssueLocation("message2", "some file", 7, 8, 5, 6, null),
            };

            var expectedFlows = new List<AnalysisIssueFlow>
            {
                new AnalysisIssueFlow(expectedLocations)
            };

            convertedIssue.Flows.Count.Should().Be(1);
            convertedIssue.Flows.Should().BeEquivalentTo(expectedFlows, config => config.WithStrictOrdering());
        }

        [TestMethod]
        [DataRow(RuleSeverity.BLOCKER, AnalysisIssueSeverity.Blocker)]
        [DataRow(RuleSeverity.CRITICAL, AnalysisIssueSeverity.Critical)]
        [DataRow(RuleSeverity.INFO, AnalysisIssueSeverity.Info)]
        [DataRow(RuleSeverity.MAJOR, AnalysisIssueSeverity.Major)]
        [DataRow(RuleSeverity.MINOR, AnalysisIssueSeverity.Minor)]
        public void ConvertFromRuleSeverity(int eslintRuleSeverity, AnalysisIssueSeverity analysisIssueSeverity)
        {
            EslintBridgeIssueConverter.Convert((RuleSeverity)eslintRuleSeverity).Should().Be(analysisIssueSeverity);
        }

        [TestMethod]
        public void ConvertFromRuleSeverity_InvalidValue_Throws()
        {
            Action act = () => EslintBridgeIssueConverter.Convert((RuleSeverity)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("ruleSeverity");
        }

        [TestMethod]
        [DataRow(RuleType.BUG, AnalysisIssueType.Bug)]
        [DataRow(RuleType.CODE_SMELL, AnalysisIssueType.CodeSmell)]
        [DataRow(RuleType.VULNERABILITY, AnalysisIssueType.Vulnerability)]
        public void ConvertFromRuleType(int eslintRuleType, AnalysisIssueType analysisIssueType)
        {
            EslintBridgeIssueConverter.Convert((RuleType) eslintRuleType).Should().Be(analysisIssueType);
        }

        [TestMethod]
        public void ConvertFromRuleType_InvalidValue_Throws()
        {
            Action act = () => EslintBridgeIssueConverter.Convert((RuleType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("ruleType");
        }

        private EslintBridgeIssueConverter CreateTestSubject(ConvertToSonarRuleKey keyMapper = null, GetRuleDefinitions getRuleDefinitions = null)
        {
            keyMapper ??= key => key;
            getRuleDefinitions ??= () => new[]
            {
                new RuleDefinition
                {
                    EslintKey = TestEslintBridgeRuleId
                }
            };

            return new EslintBridgeIssueConverter(keyMapper, getRuleDefinitions);
        }
    }
}
