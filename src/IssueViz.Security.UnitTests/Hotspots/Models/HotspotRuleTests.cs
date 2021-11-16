﻿/*
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.Models
{
    [TestClass]
    public class HotspotRuleTests
    {
        [TestMethod]
        public void Ctor_PropertiesSet()
        {
            IHotspotRule rule = new HotspotRule("key", "name", "sec cat", HotspotPriority.Medium, "risk", "vuln", "fix");

            rule.RuleKey.Should().Be("key");
            rule.RuleName.Should().Be("name");
            rule.SecurityCategory.Should().Be("sec cat");
            rule.Priority.Should().Be(HotspotPriority.Medium);
            rule.RiskDescription.Should().Be("risk");
            rule.VulnerabilityDescription.Should().Be("vuln");
            rule.FixRecommendations.Should().Be("fix");
        }
    }
}
