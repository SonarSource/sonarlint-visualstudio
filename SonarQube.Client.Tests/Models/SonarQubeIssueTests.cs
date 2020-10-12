/*
 * SonarQube Client
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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Models
{
    [TestClass]
    public class SonarQubeIssueTests
    {
        [TestMethod]
        public void Ctor_FilePathCanBeNull()
        {
            var testSubject = new SonarQubeIssue(null, "any", 1, "any", "any", "any", false, new List<IssueFlow>());

            testSubject.FilePath.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_LineCanBeNull()
        {
            var testSubject = new SonarQubeIssue("any", "any", null, "any", "any", "any", false, new List<IssueFlow>());

            testSubject.Line.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_FlowsAreNeverNull()
        {
            var testSubject = new SonarQubeIssue("any", "any", 1, "any", "any", "any", false, null);

            testSubject.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_PropertiesAreSet()
        {
            var flows = new List<IssueFlow>
            {
                new IssueFlow(null), new IssueFlow(null)
            };
            var testSubject = new SonarQubeIssue("file", "hash", 123, "message", "module", "rule", true, flows);

            testSubject.FilePath.Should().Be("file");
            testSubject.Hash.Should().Be("hash");
            testSubject.Line.Should().Be(123);
            testSubject.Message.Should().Be("message");
            testSubject.ModuleKey.Should().Be("module");
            testSubject.RuleId.Should().Be("rule");
            testSubject.IsResolved.Should().BeTrue();
            testSubject.Flows.Should().BeEquivalentTo(flows);
        }
    }
}
