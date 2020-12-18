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
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.Models
{
    [TestClass]
    public class TaintIssueTests
    {
        [TestMethod]
        public void Ctor_PropertiesSet()
        {
            var created = DateTimeOffset.Parse("2001-01-31T01:02:03+0200");
            var lastUpdated = DateTimeOffset.UtcNow;
            var issue = new TaintIssue("issue key", "local-path.cpp", "rule key", "message", 1, 2, 3, 4, "hash",
                AnalysisIssueSeverity.Major, created, lastUpdated, null);

            issue.IssueKey.Should().Be("issue key");
            issue.FilePath.Should().Be("local-path.cpp");
            issue.RuleKey.Should().Be("rule key");
            issue.Message.Should().Be("message");
            issue.StartLine.Should().Be(1);
            issue.EndLine.Should().Be(2);
            issue.StartLineOffset.Should().Be(3);
            issue.EndLineOffset.Should().Be(4);
            issue.LineHash.Should().Be("hash");
            issue.Severity.Should().Be(AnalysisIssueSeverity.Major);
            issue.CreationTimestamp.Should().Be(created);
            issue.LastUpdateTimestamp.Should().Be(lastUpdated);
        }

        [TestMethod]
        public void Ctor_NoFlows_EmptyFlows()
        {
            IReadOnlyList<IAnalysisIssueFlow> flows = null;
            var issue = new TaintIssue("issue key", "local-path.cpp", "rule key", "message", 1, 2, 3, 4, "hash",
                AnalysisIssueSeverity.Major, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, flows);

            issue.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_HasFlows_CorrectFlows()
        {
            var flows = new[] { Mock.Of<IAnalysisIssueFlow>(), Mock.Of<IAnalysisIssueFlow>() };
            var issue = new TaintIssue("issue key", "local-path.cpp", "rule key", "message", 1, 2, 3, 4, "hash",
                AnalysisIssueSeverity.Major, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, flows);

            issue.Flows.Should().BeEquivalentTo(flows);
        }
    }
}
