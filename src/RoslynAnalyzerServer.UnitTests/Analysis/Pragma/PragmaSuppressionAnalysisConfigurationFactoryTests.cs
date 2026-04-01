/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Pragma;

[TestClass]
public class PragmaSuppressionAnalysisConfigurationFactoryTests
{
    private ISonarLintSettings sonarLintSettings = null!;
    private ICurrentAnalysisIssuesStore currentAnalysisIssuesStore = null!;
    private IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarConfigurations = null!;
    private PragmaSuppressionAnalysisConfigurationFactory testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        sonarLintSettings = Substitute.For<ISonarLintSettings>();
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Warn);
        currentAnalysisIssuesStore = Substitute.For<ICurrentAnalysisIssuesStore>();
        currentAnalysisIssuesStore.GetAll().Returns([]);

        var csharpConfig = new RoslynAnalysisConfiguration(
            new SonarLintXmlConfigurationFile("any", "any"),
            ImmutableDictionary.Create<string, ReportDiagnostic>().Add("S1234", ReportDiagnostic.Warn),
            ImmutableArray.Create<DiagnosticAnalyzer>(),
            ImmutableDictionary.Create<string, IReadOnlyCollection<CodeFixProvider>>());

        sonarConfigurations = new Dictionary<RoslynLanguage, RoslynAnalysisConfiguration>
        {
            { Language.CSharp, csharpConfig }
        };

        testSubject = new PragmaSuppressionAnalysisConfigurationFactory(sonarLintSettings);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<PragmaSuppressionAnalysisConfigurationFactory, IPragmaSuppressionAnalysisConfigurationFactory>(
            MefTestHelpers.CreateExport<ISonarLintSettings>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<PragmaSuppressionAnalysisConfigurationFactory>();

    [TestMethod]
    public void Create_SeverityNone_ReturnsEmptyDictionary()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.None);

        var result = testSubject.Create(currentAnalysisIssuesStore, sonarConfigurations);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Create_SeverityInfo_ReturnsDiagnosticWithInfoSeverity()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Info);

        var result = testSubject.Create(currentAnalysisIssuesStore, sonarConfigurations);

        result.Should().NotBeEmpty();
        result[Language.CSharp].DiagnosticOptions.Should().NotBeNull();
        result[Language.CSharp].DiagnosticOptions!.Values.Should().NotContain(ReportDiagnostic.Suppress);
    }

    [TestMethod]
    public void Create_SeverityWarn_ReturnsDiagnosticWithWarnSeverity()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Warn);

        var result = testSubject.Create(currentAnalysisIssuesStore, sonarConfigurations);

        result.Should().NotBeEmpty();
        result[Language.CSharp].DiagnosticOptions.Should().NotBeNull();
        result[Language.CSharp].DiagnosticOptions!.Values.Should().NotContain(ReportDiagnostic.Suppress);
    }
}
