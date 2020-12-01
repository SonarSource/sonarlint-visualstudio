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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Models
{
    [TestClass]
    public class HotspotTests
    {
        private static readonly IHotspotRule ValidRule = CreateRule("x123");

        [TestMethod]
        public void Ctor_PropertiesSet()
        {
            var hotspot = new Hotspot("hotspot key", "local-path.cpp", "server-path", "message", 1, 2, 3, 4, "hash", ValidRule, null);

            hotspot.HotspotKey.Should().Be("hotspot key");
            hotspot.FilePath.Should().Be("local-path.cpp");
            hotspot.ServerFilePath.Should().Be("server-path");
            hotspot.Message.Should().Be("message");
            hotspot.StartLine.Should().Be(1);
            hotspot.EndLine.Should().Be(2);
            hotspot.StartLineOffset.Should().Be(3);
            hotspot.EndLineOffset.Should().Be(4);
            hotspot.LineHash.Should().Be("hash");
            hotspot.RuleKey.Should().Be(ValidRule.RuleKey);
            hotspot.Rule.Should().BeSameAs(ValidRule);
        }

        [TestMethod]
        public void Ctor_NoFlows_EmptyFlows()
        {
            IReadOnlyList<IAnalysisIssueFlow> flows = null;
            var hotspot = new Hotspot("hotspot key", "local-path.cpp", "server-path", "message", 1, 2, 3, 4, "hash", ValidRule, flows);

            hotspot.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_HasFlows_CorrectFlows()
        {
            var flows = new[] { Mock.Of<IAnalysisIssueFlow>(), Mock.Of<IAnalysisIssueFlow>() };
            var hotspot = new Hotspot("hotspot key", "local-path.cpp", "server-path", "message", 1, 2, 3, 4, "hash", ValidRule, flows);

            hotspot.Flows.Should().BeEquivalentTo(flows);
        }

        private static IHotspotRule CreateRule(string ruleKey)
        {
            var ruleMock = new Mock<IHotspotRule>();
            ruleMock.Setup(x => x.RuleKey).Returns(ruleKey);
            return ruleMock.Object;
        }
    }
}
