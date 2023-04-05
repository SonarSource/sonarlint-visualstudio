/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.Models
{
    [TestClass]
    public class TaintIssueTests
    {
        [TestMethod]
        public void Ctor_NullLocation_ArgumentNullException()
        {
            Action act = () => new TaintIssue("issue key", "rule key",
                null,
                AnalysisIssueSeverity.Major, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null, null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("primaryLocation");
        }

        [TestMethod]
        public void Ctor_PropertiesSet()
        {
            var created = DateTimeOffset.Parse("2001-01-31T01:02:03+0200");
            var lastUpdated = DateTimeOffset.UtcNow;
            var issue = new TaintIssue("issue key", "rule key",
                new AnalysisIssueLocation("message", "local-path.cpp", new TextRange(1, 2, 3, 4, "hash")),
                AnalysisIssueSeverity.Major, created, lastUpdated, null, "contextKey");

            issue.IssueKey.Should().Be("issue key");
            issue.RuleKey.Should().Be("rule key");
            issue.Severity.Should().Be(AnalysisIssueSeverity.Major);
            issue.CreationTimestamp.Should().Be(created);
            issue.LastUpdateTimestamp.Should().Be(lastUpdated);
            issue.Context.Should().Be("contextKey");

            issue.PrimaryLocation.FilePath.Should().Be("local-path.cpp");
            issue.PrimaryLocation.Message.Should().Be("message");
            issue.PrimaryLocation.TextRange.StartLine.Should().Be(1);
            issue.PrimaryLocation.TextRange.EndLine.Should().Be(2);
            issue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(3);
            issue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(4);
            issue.PrimaryLocation.TextRange.LineHash.Should().Be("hash");
        }

        [TestMethod]
        public void Ctor_NoFlows_EmptyFlows()
        {
            IReadOnlyList<IAnalysisIssueFlow> flows = null;
            var issue = new TaintIssue("issue key", "rule key",
                new AnalysisIssueLocation("message", "local-path.cpp", new TextRange(1, 2, 3, 4, "hash")),
                AnalysisIssueSeverity.Major, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, flows, null);

            issue.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_HasFlows_CorrectFlows()
        {
            var flows = new[] { Mock.Of<IAnalysisIssueFlow>(), Mock.Of<IAnalysisIssueFlow>() };
            var issue = new TaintIssue("issue key", "rule key",
                new AnalysisIssueLocation("message", "local-path.cpp", new TextRange(1, 2, 3, 4, "hash")),
                AnalysisIssueSeverity.Major, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, flows, null);

            issue.Flows.Should().BeEquivalentTo(flows);
        }
    }
}
