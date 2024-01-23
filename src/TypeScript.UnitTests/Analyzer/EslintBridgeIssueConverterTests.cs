/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;
using Edit = SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract.Edit;
using QuickFix = SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract.QuickFix;
using TextRange = SonarLint.VisualStudio.Core.Analysis.TextRange;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
{
    [TestClass]
    public class EslintBridgeIssueConverterTests
    {
        private const string ValidEsLintKey = "rule id";

        [TestMethod]
        public void Convert_IssueWithoutQuickFixes_Converted()
        {
            var eslintBridgeIssue = CreateIssue("eslint rule id");
            var ruleDefinitions = CreateRuleDefinition("eslint rule id", "sonar rule key");

            var testSubject = CreateTestSubject(ruleDefinitions);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.RuleKey.Should().Be("sonar rule key");
            convertedIssue.Type.Should().Be(AnalysisIssueType.CodeSmell);
            convertedIssue.Severity.Should().Be(AnalysisIssueSeverity.Major);

            convertedIssue.PrimaryLocation.FilePath.Should().Be("some file");
            convertedIssue.PrimaryLocation.Message.Should().Be(eslintBridgeIssue.Message);
            convertedIssue.PrimaryLocation.TextRange.StartLine.Should().Be(eslintBridgeIssue.Line);
            convertedIssue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(eslintBridgeIssue.Column);
            convertedIssue.PrimaryLocation.TextRange.EndLine.Should().Be(eslintBridgeIssue.EndLine);
            convertedIssue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(eslintBridgeIssue.EndColumn);
            convertedIssue.PrimaryLocation.TextRange.LineHash.Should().BeNull();
            convertedIssue.Fixes.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_IssueWithQuickFix_Converted()
        {
            var quickFix = CreateQuickFix();

            var eslintBridgeIssue = CreateIssue("eslint rule id", new[] { quickFix });
            var ruleDefinitions = CreateRuleDefinition("eslint rule id", "sonar rule key");

            var testSubject = CreateTestSubject(ruleDefinitions);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.Fixes.Count.Should().Be(1);
            convertedIssue.Fixes[0].Message.Should().Be(quickFix.Message);
            convertedIssue.Fixes[0].Edits.Count.Should().Be(1);
            convertedIssue.Fixes[0].Edits[0].NewText.Should().Be(quickFix.Edits[0].Text);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.StartLine.Should().Be(quickFix.Edits[0].TextRange.Line);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.EndLine.Should().Be(quickFix.Edits[0].TextRange.EndLine);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.StartLineOffset.Should().Be(quickFix.Edits[0].TextRange.Column);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.EndLineOffset.Should().Be(quickFix.Edits[0].TextRange.EndColumn);
        }

        [TestMethod]
        public void Convert_IssueWithMultipleQuickFixes_Converted()
        {
            var quickFix1 = CreateQuickFix("Quick Fix 1");
            var quickFix2 = CreateQuickFix("Quick Fix 2");

            var eslintBridgeIssue = CreateIssue("eslint rule id", new[] { quickFix1, quickFix2 });
            var ruleDefinitions = CreateRuleDefinition("eslint rule id", "sonar rule key");

            var testSubject = CreateTestSubject(ruleDefinitions);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.Fixes.Count.Should().Be(2);
            convertedIssue.Fixes[0].Message.Should().Be("Quick Fix 1");
            convertedIssue.Fixes[1].Message.Should().Be("Quick Fix 2");
        }

        [TestMethod]
        public void Convert_IssueWithQuickFix_WithMultipleEdits_Converted()
        {
            var edit1 = CreateEdit("edit 1", CreateTextRange(1, 2, 3, 4));
            var edit2 = CreateEdit("edit 2", CreateTextRange(5, 6, 7, 8));

            var quickFix = CreateQuickFix("Quick Fix", new[] { edit1, edit2 });

            var eslintBridgeIssue = CreateIssue("eslint rule id", new[] { quickFix });
            var ruleDefinitions = CreateRuleDefinition("eslint rule id", "sonar rule key");

            var testSubject = CreateTestSubject(ruleDefinitions);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.Fixes.Count.Should().Be(1);
            convertedIssue.Fixes[0].Message.Should().Be(quickFix.Message);
            convertedIssue.Fixes[0].Edits.Count.Should().Be(2);

            convertedIssue.Fixes[0].Edits[0].NewText.Should().Be(quickFix.Edits[0].Text);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.StartLine.Should().Be(quickFix.Edits[0].TextRange.Line);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.EndLine.Should().Be(quickFix.Edits[0].TextRange.EndLine);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.StartLineOffset.Should().Be(quickFix.Edits[0].TextRange.Column);
            convertedIssue.Fixes[0].Edits[0].RangeToReplace.EndLineOffset.Should().Be(quickFix.Edits[0].TextRange.EndColumn);

            convertedIssue.Fixes[0].Edits[1].NewText.Should().Be(quickFix.Edits[1].Text);
            convertedIssue.Fixes[0].Edits[1].RangeToReplace.StartLine.Should().Be(quickFix.Edits[1].TextRange.Line);
            convertedIssue.Fixes[0].Edits[1].RangeToReplace.EndLine.Should().Be(quickFix.Edits[1].TextRange.EndLine);
            convertedIssue.Fixes[0].Edits[1].RangeToReplace.StartLineOffset.Should().Be(quickFix.Edits[1].TextRange.Column);
            convertedIssue.Fixes[0].Edits[1].RangeToReplace.EndLineOffset.Should().Be(quickFix.Edits[1].TextRange.EndColumn);
        }

        [TestMethod]
        public void Convert_IssueFileLevel_Converted()
        {
            Issue eslintBridgeIssue = CreateIssue("eslint rule id");
            eslintBridgeIssue.Line = 0; //makes issue file level

            RuleDefinition[] ruleDefinitions = CreateRuleDefinition("eslint rule id", "sonar rule key");

            var testSubject = CreateTestSubject(ruleDefinitions);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.RuleKey.Should().Be("sonar rule key");
            convertedIssue.Type.Should().Be(AnalysisIssueType.CodeSmell);
            convertedIssue.Severity.Should().Be(AnalysisIssueSeverity.Major);

            convertedIssue.PrimaryLocation.FilePath.Should().Be("some file");
            convertedIssue.PrimaryLocation.Message.Should().Be(eslintBridgeIssue.Message);
            convertedIssue.PrimaryLocation.TextRange.Should().BeNull();
        }

        [TestMethod]
        [DataRow("aaa", "aaa")]
        [DataRow("aaa", "AAA")]
        [DataRow("AAA", "aaa")]
        [DataRow("AAA", "AAA")]
        public void Convert_KeyMatchingIsCaseInsensitive(string keyInIssue, string keyInDefinition)
        {
            var eslintBridgeIssue = new Issue { RuleId = keyInIssue };

            var ruleDefinitions = CreateRuleDefinition(keyInDefinition, "sonar rule key");

            var testSubject = CreateTestSubject(ruleDefinitions);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.RuleKey.Should().Be("sonar rule key");
        }

        [TestMethod]
        public void Convert_SkipsDefinitionsWithoutEsLintKey()
        {
            var eslintBridgeIssue = new Issue
            {
                RuleId = "eslint rule",
            };

            var ruleDefinitions = new[]
            {
                new RuleDefinition
                {
                    EslintKey = null,
                    RuleKey = "known sonar1",
                },
                new RuleDefinition
                {
                    EslintKey = "eslint rule",
                    RuleKey = "known sonar2",
                }
            };

            var testSubject = CreateTestSubject(ruleDefinitions);

            var result = testSubject.Convert("some file", eslintBridgeIssue);

            result.Should().NotBeNull();
            result.RuleKey.Should().Be("known sonar2");
        }

        [TestMethod]
        public void Convert_UnrecognisedESLintKey_Throws()
        {
            // This test demonstrates what will happen if eslintbridge returns a rule with
            // an unrecognised rule -> exception throw.
            // We don't expect it to happen in practice, since eslint should only run the rules we ask it to
            // run. However, this test shows we'll get an error in that case -> issues will be lost.

            var eslintBridgeIssue = new Issue
            {
                RuleId = "unknown eslint rule",
            };

            var ruleDefinitions = new[]
            {
                new RuleDefinition
                {
                    EslintKey = "known eslint",
                    RuleKey = "known sonar",
                }
            };

            var testSubject = CreateTestSubject(ruleDefinitions);

            Action act = () => testSubject.Convert("some file", eslintBridgeIssue);
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void Convert_NoSecondaryLocations_IssueWithoutFlows()
        {
            var eslintBridgeIssue = new Issue
            {
                RuleId = ValidEsLintKey,
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
                RuleId = ValidEsLintKey,
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
                new("message1", "some file", new TextRange(3, 4, 1, 2, null)),
                new("message2", "some file", new TextRange(7, 8, 5, 6, null)),
            };

            var expectedFlows = new List<AnalysisIssueFlow>
            {
                new(expectedLocations)
            };

            convertedIssue.Flows.Count.Should().Be(1);
            convertedIssue.Flows.Should().BeEquivalentTo(expectedFlows, config => config.WithStrictOrdering());
        }

        [TestMethod]
        [DataRow("aaa", "aaa")]
        [DataRow("aaa", "AAA")]
        [DataRow("AAA", "aaa")]
        [DataRow("AAA", "AAA")]
        public void Convert_HasNoEsLintKey_UsesStylelintKey(string keyInIssue, string keyInDefinition)
        {
            var eslintBridgeIssue = new Issue { RuleId = keyInIssue };

            var ruleDefinitions = CreateRuleDefinition(null, "sonar rule key", keyInDefinition);

            var testSubject = CreateTestSubject(ruleDefinitions);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.RuleKey.Should().Be("sonar rule key");
        }

        [TestMethod]
        [DataRow("CssSyntaxError")]
        [DataRow("csssyntaxerror")]
        [DataRow("CSSSYNTAXERROR")]
        public void Convert_HasCssSyntaxError_ReturnsNull(string ruleID)
        {
            var eslintBridgeIssue = new Issue { RuleId = ruleID };

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(logger: logger);

            var result = testSubject.Convert("some file", eslintBridgeIssue);

            result.Should().BeNull();
            logger.AssertPartialOutputStringExists("Failed to parse css file 'some file'");
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
        [DataRow(RuleType.SECURITY_HOTSPOT, AnalysisIssueType.SecurityHotspot)]
        public void ConvertFromRuleType(int eslintRuleType, AnalysisIssueType analysisIssueType)
        {
            EslintBridgeIssueConverter.Convert((RuleType)eslintRuleType).Should().Be(analysisIssueType);
        }

        [TestMethod]
        public void ConvertFromRuleType_InvalidValue_Throws()
        {
            Action act = () => EslintBridgeIssueConverter.Convert((RuleType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("ruleType");
        }

        private EslintBridgeIssueConverter CreateTestSubject(IEnumerable<RuleDefinition> ruleDefinitions = null, ILogger logger = null)
        {
            ruleDefinitions ??= new[]
            {
                new RuleDefinition
                {
                    EslintKey = ValidEsLintKey
                }
            };

            var rulesProvider = new Mock<IRulesProvider>();
            rulesProvider.Setup(x => x.GetDefinitions()).Returns(ruleDefinitions);

            logger = logger ?? Mock.Of<ILogger>();

            return new EslintBridgeIssueConverter(rulesProvider.Object, logger);
        }

        private static RuleDefinition[] CreateRuleDefinition(string eslintKey, string ruleKey, string stylelintKey = null)
        {
            return new[]
            {
                new RuleDefinition
                {
                    EslintKey = eslintKey,
                    RuleKey = ruleKey,
                    Type = RuleType.CODE_SMELL,
                    Severity = RuleSeverity.MAJOR,
                    StylelintKey = stylelintKey
                }
            };
        }

        private static Issue CreateIssue(string ruleId, QuickFix[] quickFixes = null)
        {
            return new Issue
            {
                RuleId = ruleId,
                Column = 1,
                EndColumn = 2,
                Line = 3,
                EndLine = 4,
                Message = "some message",
                QuickFixes = quickFixes
            };
        }

        private static QuickFix CreateQuickFix(string message = "QuickFix", Edit[] edits = null)
        {
            return new QuickFix
            {
                Message = message,
                Edits = edits ?? new[] { CreateEdit() }
            };
        }

        private static Edit CreateEdit(string text = "edit", TypeScript.EslintBridgeClient.Contract.TextRange textRange = null)
        {
            return new Edit
            {
                Text = text,
                TextRange = textRange ?? CreateTextRange(1, 2, 3, 4)
            };
        }

        private static TypeScript.EslintBridgeClient.Contract.TextRange CreateTextRange(int column, int endColumn, int line, int endLine)
        {
            return new TypeScript.EslintBridgeClient.Contract.TextRange
            {
                Column = column,
                EndColumn = endColumn,
                Line = line,
                EndLine = endLine
            };
        }
    }
}
