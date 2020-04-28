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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.UnitTests.CSharpVB
{
    [TestClass]
    public class RoslynPluginRuleKeyExtensionsTests
    {
        [TestMethod]
        [DataRow("roslyn.x", "x")]
        [DataRow("roslyn.ANYTHING","ANYTHING")]
        [DataRow("roslyn.", null)]
        [DataRow("roslynx.wrongprefix", null)]
        [DataRow("ROSLYN.case.sensitive", null)]
        [DataRow("csharpsquid", "sonaranalyzer-cs")] // special case - SonarC#
        [DataRow("vbnet", "sonaranalyzer-vbnet")] // special case for SonarVBNet
        public void TryGetPrefix(string ruleKey, string expectedPrefix)
        {
            var rule = new SonarQubeRule("any", ruleKey, false, SonarQubeIssueSeverity.Unknown, null);

            rule.TryGetRoslynPluginPropertyPrefix().Should().Be(expectedPrefix);
        }
    }
}
