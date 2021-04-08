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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
{
    [TestClass]
    public class EslintBridgeIssueConverterTests
    {
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

            var testSubject = CreateTestSubject(keyMapper);
            var convertedIssue = testSubject.Convert("some file", eslintBridgeIssue);

            convertedIssue.RuleKey.Should().Be("mapped rule id");
            convertedIssue.StartLine.Should().Be(3);
            convertedIssue.StartLineOffset.Should().Be(1);
            convertedIssue.EndLine.Should().Be(4);
            convertedIssue.EndLineOffset.Should().Be(2);
            convertedIssue.Message.Should().Be("some message");
            convertedIssue.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void Convert_NoSecondaryLocations_IssueWithoutFlows()
        {
            var eslintBridgeIssue = new Issue
            {
                RuleId = "rule id",
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

        private EslintBridgeIssueConverter CreateTestSubject(ConvertToSonarRuleKey keyMapper = null)
        {
            keyMapper ??= key => key;
            return new EslintBridgeIssueConverter(keyMapper);
        }
    }
}
