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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

[Export(typeof(IPragmaSuppressionAnalysisConfigurationFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class PragmaSuppressionAnalysisConfigurationFactory(ISonarLintSettings sonarLintSettings)
    : IPragmaSuppressionAnalysisConfigurationFactory
{
    public IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> Create(
        ICurrentAnalysisIssuesStore currentAnalysisIssuesStore,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations)
    {
        if (sonarLintSettings.PragmaRuleSeverity == PragmaRuleSeverity.None)
        {
            return new Dictionary<RoslynLanguage, RoslynAnalysisConfiguration>();
        }

        var diagnosticAwarePragmaAnalyzer = new DiagnosticAwarePragmaAnalyzer(
            currentAnalysisIssuesStore.GetAll,
            sonarRoslynAnalysisConfigurations[Language.CSharp].DiagnosticOptions!.Keys.ToImmutableHashSet());
        var pragmaWarningDisableCodeFixProvider = new PragmaWarningDisableCodeFixProvider();
        IReadOnlyCollection<CodeFixProvider> codeFixes = new List<CodeFixProvider> { pragmaWarningDisableCodeFixProvider };

        return new Dictionary<RoslynLanguage, RoslynAnalysisConfiguration>
        {
            {
                Language.CSharp,
                new RoslynAnalysisConfiguration(
                    sonarRoslynAnalysisConfigurations[Language.CSharp].SonarLintXml, // strictly speaking this is not needed, but is kept to make fewer changes to compilation provider
                    diagnosticAwarePragmaAnalyzer.SupportedDiagnostics.ToImmutableDictionary(x => x.Id, _ => ReportDiagnostic.Warn),
                    ImmutableArray.Create<DiagnosticAnalyzer>(diagnosticAwarePragmaAnalyzer),
                    pragmaWarningDisableCodeFixProvider.FixableDiagnosticIds.ToImmutableDictionary(x => x, _ => codeFixes))
            }
        };
    }
}
