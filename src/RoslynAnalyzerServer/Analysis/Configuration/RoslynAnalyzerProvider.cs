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
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

[Export(typeof(IRoslynAnalyzerProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynAnalyzerProvider(IEmbeddedDotnetAnalyzersLocator analyzersLocator, IRoslynAnalyzerLoader roslynAnalyzerLoader) : IRoslynAnalyzerProvider
{
    private ImmutableDictionary<LicensedRoslynLanguage, AnalyzerAssemblyContents>? cachedAnalyzerAssemblyContents;

    public ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents> LoadAndProcessAnalyzerAssemblies(AnalyzerInfoDto analyzerInfo)
    {
        if (cachedAnalyzerAssemblyContents == null)
        {
            var analyzerFullPathsByLanguage = analyzersLocator.GetAnalyzerFullPathsByLicensedLanguage();
            cachedAnalyzerAssemblyContents = LoadFromAssemblies(analyzerFullPathsByLanguage);
        }

        return cachedAnalyzerAssemblyContents
            .Where(kvp => FilterByLicense(kvp, analyzerInfo))
            .ToDictionary(kvp => kvp.Key.RoslynLanguage, kvp => kvp.Value)
            .ToImmutableDictionary();
        ;
    }

    private static bool FilterByLicense(KeyValuePair<LicensedRoslynLanguage, AnalyzerAssemblyContents> kvp, AnalyzerInfoDto analyzerInfo)
    {
        if (kvp.Key.RoslynLanguage.Equals(Language.VBNET))
        {
            return kvp.Key.IsEnterprise == analyzerInfo.ShouldUseVbEnterprise;
        }

        return kvp.Key.IsEnterprise == analyzerInfo.ShouldUseCsharpEnterprise;
    }

    private ImmutableDictionary<LicensedRoslynLanguage, AnalyzerAssemblyContents> LoadFromAssemblies(Dictionary<LicensedRoslynLanguage, List<string>> analyzerFullPathsByLanguage)
    {
        var builder = ImmutableDictionary.CreateBuilder<LicensedRoslynLanguage, AnalyzerAssemblyContents>();

        foreach (var languageAndAnalyzers in analyzerFullPathsByLanguage)
        {
            var supportedDiagnostics = ImmutableHashSet.CreateBuilder<string>();
            var analyzers = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            var codeFixProviders = ImmutableDictionary.CreateBuilder<string, IReadOnlyCollection<CodeFixProvider>>();

            foreach (var assemblyContents in languageAndAnalyzers.Value.Select(roslynAnalyzerLoader.LoadAnalyzerAssembly))
            {
                analyzers.AddRange(assemblyContents.Analyzers);
                supportedDiagnostics.UnionWith(assemblyContents.Analyzers.SelectMany(x => x.SupportedDiagnostics.Select(y => y.Id)));
                AddCodeFixProviders(assemblyContents, codeFixProviders);
            }

            var immutableArray = supportedDiagnostics.ToImmutable();
            builder.Add(languageAndAnalyzers.Key, new AnalyzerAssemblyContents(analyzers.ToImmutable(), immutableArray.ToImmutableHashSet(), codeFixProviders.ToImmutable()));
        }

        return builder.ToImmutable();
    }

    private static void AddCodeFixProviders(LoadedAnalyzerClasses classes, ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>>.Builder codeFixProviders)
    {
        foreach (var codeFixProvider in classes.CodeFixProviders)
        {
            foreach (var fixableDiagnosticId in codeFixProvider.FixableDiagnosticIds)
            {
                if (!codeFixProviders.ContainsKey(fixableDiagnosticId))
                {
                    codeFixProviders[fixableDiagnosticId] = new List<CodeFixProvider>();
                }
                ((List<CodeFixProvider>)codeFixProviders[fixableDiagnosticId]).Add(codeFixProvider);
            }
        }
    }
}
