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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

[Export(typeof(IBasicRoslynAnalyzerProvider))]
[Export(typeof(IEnterpriseRoslynAnalyzerProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class EmbeddedDotnetAnalyzerProvider(
    IObsoleteDotnetAnalyzersLocator locator,
    IAnalyzerAssemblyLoaderFactory analyzerAssemblyLoaderFactory,
    IConfigurationScopeDotnetAnalyzerIndicator indicator,
    ILogger logger,
    IThreadHandling threadHandling)
    : IBasicRoslynAnalyzerProvider, IEnterpriseRoslynAnalyzerProvider
{
    private readonly IAnalyzerAssemblyLoader loader = analyzerAssemblyLoaderFactory.Create();
    private ImmutableArray<AnalyzerFileReference>? basicAnalyzers;
    private ImmutableArray<AnalyzerFileReference>? enterpriseAnalyzers;

    public Task<ImmutableArray<AnalyzerFileReference>> GetBasicAsync() =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            basicAnalyzers ??= CreateAnalyzerFileReferences(locator.GetBasicAnalyzerFullPaths());

            return Task.FromResult(basicAnalyzers.Value);
        });

    public Task<ImmutableArray<AnalyzerFileReference>?> GetEnterpriseOrNullAsync(string configurationScopeId) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!await indicator.ShouldUseEnterpriseCSharpAnalyzerAsync(configurationScopeId))
            {
                return null;
            }

            enterpriseAnalyzers ??= CreateAnalyzerFileReferences(locator.GetEnterpriseAnalyzerFullPaths());
            return enterpriseAnalyzers;

        });

    private ImmutableArray<AnalyzerFileReference> CreateAnalyzerFileReferences(List<string> analyzerPaths)
    {
        if (analyzerPaths.Count == 0)
        {
            logger.LogVerbose(Resources.EmbeddedRoslynAnalyzersNotFound);
            throw new InvalidOperationException(Resources.EmbeddedRoslynAnalyzersNotFound);
        }

        return analyzerPaths.Select(path => new AnalyzerFileReference(path, loader)).ToImmutableArray();
    }
}
