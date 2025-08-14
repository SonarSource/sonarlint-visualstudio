/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class RuleStatusProviderTests
{
    private readonly RuleStatusProvider testSubject = new();

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RuleStatusProvider, IRuleStatusProvider>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RuleStatusProvider>();

    [TestMethod]
    public void GetDiagnosticOptions_EmptyDiagnosticIds_ReturnsEmptyDictionary()
    {
        var result = testSubject.GetDiagnosticOptions([], []);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetDiagnosticOptions_EmptyActiveDiagnosticIds_SuppressesAllDiagnostics()
    {
        var diagnosticIds = new[] { "Rule1", "Rule2", "Rule3" };
        var activeDiagnosticIds = new HashSet<string>();

        var result = testSubject.GetDiagnosticOptions(diagnosticIds, activeDiagnosticIds);

        result.Count.Should().Be(3);
        result["Rule1"].Should().Be(ReportDiagnostic.Suppress);
        result["Rule2"].Should().Be(ReportDiagnostic.Suppress);
        result["Rule3"].Should().Be(ReportDiagnostic.Suppress);
    }

    [TestMethod]
    public void GetDiagnosticOptions_SomeActiveDiagnosticIds_ConfiguresCorrectly()
    {
        var diagnosticIds = new[] { "Rule1", "Rule2", "Rule3", "Rule4" };
        var activeDiagnosticIds = new HashSet<string> { "Rule1", "Rule3" };

        var result = testSubject.GetDiagnosticOptions(diagnosticIds, activeDiagnosticIds);

        result.Count.Should().Be(4);
        result["Rule1"].Should().Be(ReportDiagnostic.Warn);
        result["Rule2"].Should().Be(ReportDiagnostic.Suppress);
        result["Rule3"].Should().Be(ReportDiagnostic.Warn);
        result["Rule4"].Should().Be(ReportDiagnostic.Suppress);
    }

    [TestMethod]
    public void GetDiagnosticOptions_AllActiveDiagnosticIds_EnablesAllRules()
    {
        var diagnosticIds = new[] { "Rule1", "Rule2" };
        var activeDiagnosticIds = new HashSet<string> { "Rule1", "Rule2" };

        var result = testSubject.GetDiagnosticOptions(diagnosticIds, activeDiagnosticIds);

        result.Count.Should().Be(2);
        result["Rule1"].Should().Be(ReportDiagnostic.Warn);
        result["Rule2"].Should().Be(ReportDiagnostic.Warn);
    }

    [TestMethod]
    public void GetDiagnosticOptions_ActiveIdsContainNonExistentRules_OnlyConfiguresExistingRules()
    {
        var diagnosticIds = new[] { "Rule1", "Rule2" };
        var activeDiagnosticIds = new HashSet<string> { "Rule1", "NonExistentRule" };

        var result = testSubject.GetDiagnosticOptions(diagnosticIds, activeDiagnosticIds);

        result.Count.Should().Be(2);
        result["Rule1"].Should().Be(ReportDiagnostic.Warn);
        result["Rule2"].Should().Be(ReportDiagnostic.Suppress);
        result.Keys.Should().NotContain("NonExistentRule");
    }

    [TestMethod]
    public void GetDiagnosticOptions_ReturnsDictionaryType_IsImmutable()
    {
        var diagnosticIds = new[] { "Rule1" };
        var activeDiagnosticIds = new HashSet<string>();

        var result = testSubject.GetDiagnosticOptions(diagnosticIds, activeDiagnosticIds);

        result.Should().BeOfType<ImmutableDictionary<string, ReportDiagnostic>>();
    }
}

