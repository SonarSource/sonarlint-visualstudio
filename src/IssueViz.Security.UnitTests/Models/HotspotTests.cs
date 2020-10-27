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
        [TestMethod]
        public void Ctor_PropertiesSet()
        {
            var hotspot = new Hotspot("a.cpp", "message", 1, 2, 3, 4, "hash", "rule", null);

            hotspot.FilePath.Should().Be("a.cpp");
            hotspot.Message.Should().Be("message");
            hotspot.StartLine.Should().Be(1);
            hotspot.EndLine.Should().Be(2);
            hotspot.StartLineOffset.Should().Be(3);
            hotspot.EndLineOffset.Should().Be(4);
            hotspot.LineHash.Should().Be("hash");
            hotspot.RuleKey.Should().Be("rule");
        }

        [TestMethod]
        public void Ctor_NoFlows_EmptyFlows()
        {
            IReadOnlyList<IAnalysisIssueFlow> flows = null;
            var hotspot = new Hotspot("a.cpp", "message", 1, 2, 3, 4, "hash", "rule", flows);

            hotspot.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_HasFlows_CorrectFlows()
        {
            var flows = new[] { Mock.Of<IAnalysisIssueFlow>(), Mock.Of<IAnalysisIssueFlow>() };
            var hotspot = new Hotspot("a.cpp", "message", 1, 2, 3, 4, "hash", "rule", flows);

            hotspot.Flows.Should().BeEquivalentTo(flows);
        }
    }
}
