/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class RoslynRuleConfigurationTests
{
    [TestMethod]
    public void Constructor_InitializesProperties()
    {
        var ruleId = new SonarCompositeRuleId("cpp", "rule1");
        var parameters = new Dictionary<string, string> { { "param1", "value1" }, { "param2", "value2" } };
        var isActive = true;

        var testSubject = new RoslynRuleConfiguration(ruleId, isActive, parameters);

        testSubject.RuleId.Should().Be(ruleId);
        testSubject.IsActive.Should().Be(isActive);
        testSubject.Parameters.Should().BeSameAs(parameters);
    }

    [TestMethod]
    public void Constructor_NullParameters_AcceptsNull()
    {
        var ruleId = new SonarCompositeRuleId("cpp", "rule1");
        var isActive = true;

        var testSubject = new RoslynRuleConfiguration(ruleId, isActive, null);

        testSubject.RuleId.Should().Be(ruleId);
        testSubject.IsActive.Should().Be(isActive);
        testSubject.Parameters.Should().BeNull();
    }

    [TestMethod]
    public void ReportDiagnostic_WhenActive_ReturnsWarn()
    {
        var ruleId = new SonarCompositeRuleId("cpp", "rule1");
        var testSubject = new RoslynRuleConfiguration(ruleId, true, null);

        testSubject.ReportDiagnostic.Should().Be(ReportDiagnostic.Warn);
    }

    [TestMethod]
    public void ReportDiagnostic_WhenInactive_ReturnsSuppress()
    {
        var ruleId = new SonarCompositeRuleId("cpp", "rule1");
        var testSubject = new RoslynRuleConfiguration(ruleId, false, null);

        testSubject.ReportDiagnostic.Should().Be(ReportDiagnostic.Suppress);
    }
}
