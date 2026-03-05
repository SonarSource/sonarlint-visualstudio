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

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

[Export(typeof(IAdditionalAnalysisConfigurationFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AdditionalAnalysisConfigurationFactory : IAdditionalAnalysisConfigurationFactory
{
    public IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> Create(
        Func<ImmutableArray<Diagnostic>> knownIssuesProvider,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations)
    {
        var diagnosticAwarePragmaAnalyzer = new DiagnosticAwarePragmaAnalyzer(
            knownIssuesProvider,
            sonarRoslynAnalysisConfigurations[Language.CSharp].DiagnosticOptions!.Keys.ToImmutableHashSet());
        return new Dictionary<RoslynLanguage, RoslynAnalysisConfiguration>
        {
            {
                Language.CSharp,
                new RoslynAnalysisConfiguration(
                    sonarRoslynAnalysisConfigurations[Language.CSharp].SonarLintXml, // strictly speaking this is not needed, but is kept to make fewer changes to compilation provider
                    diagnosticAwarePragmaAnalyzer.SupportedDiagnostics.ToImmutableDictionary(x => x.Id, _ => ReportDiagnostic.Warn),
                    ImmutableArray.Create<DiagnosticAnalyzer>(diagnosticAwarePragmaAnalyzer),
                    ImmutableDictionary.Create<string, IReadOnlyCollection<CodeFixProvider>>()
                        .Add(DiagnosticAwarePragmaAnalyzer.DiagnosticId, [new PragmaWarningDisableCodeFixProvider()]))
            }
        };
    }
}
