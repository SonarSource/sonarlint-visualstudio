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
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Models
{
    [TestClass]
    public class SonarQubeIssueTests
    {
        private static readonly DateTimeOffset ValidTimestamp = DateTimeOffset.UtcNow;

        [TestMethod]
        public void Ctor_FilePathCanBeNull()
        {
            var testSubject = new SonarQubeIssue("issueKey", null, "hash", "message", "module", "rule", true,
                SonarQubeIssueSeverity.Info, ValidTimestamp, ValidTimestamp, textRange: null, flows: null);

            testSubject.FilePath.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_TextRangeCanBeNull()
        {
            var testSubject = new SonarQubeIssue("issueKey", "file", "hash", "message", "module", "rule", true,
                SonarQubeIssueSeverity.Info, ValidTimestamp, ValidTimestamp, textRange: null, flows: null);

            testSubject.TextRange.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_FlowsAreNeverNull()
        {
            var testSubject = new SonarQubeIssue("issueKey", "file", "hash", "message", "module", "rule", true,
                SonarQubeIssueSeverity.Info, ValidTimestamp, ValidTimestamp, new IssueTextRange(123, 456, 7, 8), flows: null);

            testSubject.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_PropertiesAreSet()
        {
            var creationTimestamp = DateTimeOffset.Parse("2001-12-13T10:11:12+0000");
            var lastUpdateTimestamp = DateTimeOffset.Parse("2020-01-02T13:14:15+0000");

            var flows = new List<IssueFlow>
            {
                new IssueFlow(null), new IssueFlow(null)
            };
            var testSubject = new SonarQubeIssue("issueKey", "file", "hash", "message", "module", "rule", true, SonarQubeIssueSeverity.Info,
                creationTimestamp, lastUpdateTimestamp, new IssueTextRange(123, 456, 7, 8), flows, "contextKey");

            testSubject.IssueKey.Should().Be("issueKey");
            testSubject.FilePath.Should().Be("file");
            testSubject.Hash.Should().Be("hash");
            testSubject.Message.Should().Be("message");
            testSubject.ModuleKey.Should().Be("module");
            testSubject.RuleId.Should().Be("rule");
            testSubject.IsResolved.Should().BeTrue();
            testSubject.Severity.Should().Be(SonarQubeIssueSeverity.Info);
            testSubject.CreationTimestamp.Should().Be(creationTimestamp);
            testSubject.LastUpdateTimestamp.Should().Be(lastUpdateTimestamp);
            testSubject.TextRange.Should().BeEquivalentTo(new IssueTextRange(123, 456, 7, 8));
            testSubject.Flows.Should().BeEquivalentTo(flows);
            testSubject.Context.Should().Be("contextKey");
        }
    }
}
