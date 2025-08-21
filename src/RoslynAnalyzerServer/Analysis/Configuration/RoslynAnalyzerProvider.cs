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
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

[Export(typeof(IRoslynAnalyzerProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynAnalyzerProvider(IEmbeddedDotnetAnalyzersLocator analyzersLocator, IRoslynAnalyzerLoader roslynAnalyzerLoader) : IRoslynAnalyzerProvider
{
    public ImmutableDictionary<Language, AnalyzersAndSupportedRules> GetAnalyzersByLanguage() =>
        // todo SLVS-2410 Respect NET repackaging
        LoadAnalyzersAndRules(analyzersLocator.GetBasicAnalyzerFullPathsByLanguage());

    private ImmutableDictionary<Language, AnalyzersAndSupportedRules> LoadAnalyzersAndRules(Dictionary<Language, List<string>> analyzerFullPathsByLanguage)
    {
        var builder = ImmutableDictionary.CreateBuilder<Language, AnalyzersAndSupportedRules>();

        foreach (var languageAndAnalyzers in analyzerFullPathsByLanguage)
        {
            var supportedDiagnostics = ImmutableHashSet.CreateBuilder<string>();
            var analyzers = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

            foreach (var diagnosticAnalyzer in languageAndAnalyzers.Value.SelectMany(roslynAnalyzerLoader.LoadAnalyzers))
            {
                analyzers.Add(diagnosticAnalyzer);
                supportedDiagnostics.UnionWith(diagnosticAnalyzer.SupportedDiagnostics.Select(x => x.Id));
            }

            builder.Add(languageAndAnalyzers.Key, new AnalyzersAndSupportedRules(analyzers.ToImmutable(), supportedDiagnostics.ToImmutable()));
        }

        return builder.ToImmutable();
    }
}
