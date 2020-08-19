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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyIssueToAnalysisIssueConverterTests
    {
        private CFamilyIssueToAnalysisIssueConverter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new CFamilyIssueToAnalysisIssueConverter();
        }

        [TestMethod]
        public void Convert_NoMessageParts_IssueWithoutFlows()
        {
            var message = new Message("rule2",
                "file",
                4, 3,
                2,
                1,
                "this is a test",
                false,
                new MessagePart[0]);

            // Act
            var issue = Convert(message);

            // Assert
            issue.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_HasMessageParts_IssueWithSingleFlow()
        {
            var messageParts = new List<MessagePart>
            {
                new MessagePart("test1.cpp", 1, 2, 3, 4, "this is a test 1"),
                new MessagePart("test2.cpp", 5, 6, 7, 8, "this is a test 2")
            };

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("this is a test 2", "test2.cpp", 5, 7, 5, 7, null),
                new AnalysisIssueLocation("this is a test 1", "test1.cpp", 1, 3, 1, 3, null)
            };

            var expectedFlows = new List<AnalysisIssueFlow>
            {
                new AnalysisIssueFlow(expectedLocations)
            };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            // Act
            var issue = Convert(message);

            // Assert
            issue.Flows.Should().BeEquivalentTo(expectedFlows, x => x.WithStrictOrdering());
        }

        [TestMethod]
        public void Convert_EndLineIsNotZero()
        {
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new MessagePart[0]);

            // Act
            var issue = Convert(message);

            // Assert
            issue.StartLine.Should().Be(4);
            issue.StartLineOffset.Should().Be(3 - 1);

            issue.EndLine.Should().Be(2);
            issue.EndLineOffset.Should().Be(1 - 1);

            issue.RuleKey.Should().Be("lang1:rule2");
            issue.FilePath.Should().Be("file");
            issue.Message.Should().Be("test endline is not zero");
        }

        [TestMethod]
        public void Convert_EndLineIsZero()
        {
            // Special case: ignore column offsets if EndLine is zero
            var message = new Message("rule2", "ff", 101, 1, 0, 3, "test endline is zero", true, new MessagePart[0]);

            // Act
            var issue = Convert(message);

            // Assert
            issue.StartLine.Should().Be(101);

            issue.EndLine.Should().Be(0);
            issue.StartLineOffset.Should().Be(0);
            issue.EndLineOffset.Should().Be(0);

            issue.RuleKey.Should().Be("lang1:rule2");
            issue.FilePath.Should().Be("ff");
            issue.Message.Should().Be("test endline is zero");
        }

        [TestMethod]
        [DataRow("rule2", AnalysisIssueSeverity.Info, AnalysisIssueType.CodeSmell)]
        [DataRow("rule3", AnalysisIssueSeverity.Critical, AnalysisIssueType.Vulnerability)]
        public void Convert_SeverityAndTypeLookup(string ruleKey, AnalysisIssueSeverity severity, AnalysisIssueType type)
        {
            var message = new Message(ruleKey, "any", 4, 3, 2, 1, "message", false, new MessagePart[0]);
            var issue = Convert(message);

            issue.RuleKey.Should().Be($"lang1:{ruleKey}");
            issue.Severity.Should().Be(severity);
            issue.Type.Should().Be(type);
        }

        [TestMethod]
        [DataRow(IssueSeverity.Blocker, AnalysisIssueSeverity.Blocker)]
        [DataRow(IssueSeverity.Critical, AnalysisIssueSeverity.Critical)]
        [DataRow(IssueSeverity.Info, AnalysisIssueSeverity.Info)]
        [DataRow(IssueSeverity.Major, AnalysisIssueSeverity.Major)]
        [DataRow(IssueSeverity.Minor, AnalysisIssueSeverity.Minor)]
        public void ConvertFromIssueSeverity(IssueSeverity cfamilySeverity, AnalysisIssueSeverity analysisIssueSeverity)
        {
            CFamilyIssueToAnalysisIssueConverter.Convert(cfamilySeverity).Should().Be(analysisIssueSeverity);
        }

        [TestMethod]
        public void ConvertFromIssueSeverity_InvalidValue_Throws()
        {
            Action act = () => CFamilyIssueToAnalysisIssueConverter.Convert((IssueSeverity)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueSeverity");
        }

        [TestMethod]
        [DataRow(IssueType.Bug, AnalysisIssueType.Bug)]
        [DataRow(IssueType.CodeSmell, AnalysisIssueType.CodeSmell)]
        [DataRow(IssueType.Vulnerability, AnalysisIssueType.Vulnerability)]
        public void ConvertFromIssueType(IssueType cfamilyIssueType, AnalysisIssueType analysisIssueType)
        {
            CFamilyIssueToAnalysisIssueConverter.Convert(cfamilyIssueType).Should().Be(analysisIssueType);

            Action act = () => CFamilyIssueToAnalysisIssueConverter.Convert((IssueType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        [TestMethod]
        public void ConvertFromIssueType_InvalidValue_Throws()
        {
            Action act = () => CFamilyIssueToAnalysisIssueConverter.Convert((IssueType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        private static ICFamilyRulesConfig GetDummyRulesConfiguration()
        {
            var config = new DummyCFamilyRulesConfig("any")
                .AddRule("rule1", IssueSeverity.Blocker, isActive: false,
                    parameters: new Dictionary<string, string>
                        {{"rule1 Param1", "rule1 Value1"}, {"rule1 Param2", "rule1 Value2"}})
                .AddRule("rule2", IssueSeverity.Info, isActive: true,
                    parameters: new Dictionary<string, string>
                        {{"rule2 Param1", "rule2 Value1"}, {"rule2 Param2", "rule2 Value2"}})
                .AddRule("rule3", IssueSeverity.Critical, isActive: true,
                    parameters: new Dictionary<string, string>
                        {{"rule3 Param1", "rule3 Value1"}, {"rule3 Param2", "rule3 Value2"}});

            config.RulesMetadata["rule1"].Type = IssueType.Bug;
            config.RulesMetadata["rule2"].Type = IssueType.CodeSmell;
            config.RulesMetadata["rule3"].Type = IssueType.Vulnerability;

            return config;
        }

        private IAnalysisIssue Convert(Message message)
        {
            return testSubject.Convert(message, "lang1", GetDummyRulesConfiguration());
        }
    }
}
