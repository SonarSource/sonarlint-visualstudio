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
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

public interface ISolutionRoslynAnalyzerManager : IDisposable
{
}

[Export(typeof(ISolutionRoslynAnalyzerManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SolutionRoslynAnalyzerManager : ISolutionRoslynAnalyzerManager
{
    private static readonly ImmutableArray<AnalyzerFileReference> NoChange = ImmutableArray<AnalyzerFileReference>.Empty;
    private bool disposed;
    private readonly IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IActiveSolutionTracker activeSolutionTracker;
    private readonly IAsyncLock asyncLock;
    private readonly IEnterpriseRoslynAnalyzerProvider enterpriseAnalyzerProvider;
    private readonly IBasicRoslynAnalyzerProvider basicAnalyzerProvider;
    private readonly ILogger logger;
    private readonly IRoslynWorkspaceWrapper roslynWorkspace;
    private ImmutableArray<AnalyzerFileReference>? currentAnalyzers;

    [ImportingConstructor]
    public SolutionRoslynAnalyzerManager(
        IBasicRoslynAnalyzerProvider basicAnalyzerProvider,
        IEnterpriseRoslynAnalyzerProvider enterpriseAnalyzerProvider,
        IRoslynWorkspaceWrapper roslynWorkspace,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IActiveSolutionTracker activeSolutionTracker,
        IAsyncLockFactory asyncLockFactory,
        ILogger logger)
        : this(basicAnalyzerProvider,
            enterpriseAnalyzerProvider,
            roslynWorkspace,
            AnalyzerArrayComparer.Instance,
            activeConfigScopeTracker,
            activeSolutionTracker,
            asyncLockFactory,
            logger)
    {
    }

    internal /* for testing */ SolutionRoslynAnalyzerManager(
        IBasicRoslynAnalyzerProvider basicAnalyzerProvider,
        IEnterpriseRoslynAnalyzerProvider enterpriseAnalyzerProvider,
        IRoslynWorkspaceWrapper roslynWorkspace,
        IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IActiveSolutionTracker activeSolutionTracker,
        IAsyncLockFactory asyncLockFactory,
        ILogger logger)
    {
        this.basicAnalyzerProvider = basicAnalyzerProvider;
        this.enterpriseAnalyzerProvider = enterpriseAnalyzerProvider;
        this.roslynWorkspace = roslynWorkspace;
        this.analyzerComparer = analyzerComparer;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.activeSolutionTracker = activeSolutionTracker;
        this.asyncLock = asyncLockFactory.Create();
        this.logger = logger.ForVerboseContext("Roslyn Analyzers");

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += OnConfigurationScopeChanged;
        activeSolutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;
    }

    internal /*for testing*/ async Task OnSolutionStateChangedAsync(string solutionName)
    {
        using (await asyncLock.AcquireAsync())
        {
            ThrowIfDisposed();

            if (solutionName is null)
            {
                await RemoveCurrentAnalyzersAsync();
                return;
            }
            try
            {
                await UpdateAnalyzersIfChangedAsync(await ChooseAnalyzersAsync(solutionName));
            }
            catch (Exception e)
            {
                logger.WriteLine(e.ToString());
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SolutionRoslynAnalyzerManager));
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        activeConfigScopeTracker.CurrentConfigurationScopeChanged -= OnConfigurationScopeChanged;
        activeSolutionTracker.ActiveSolutionChanged -= OnActiveSolutionChanged;
        disposed = true;
    }

    private async Task<ImmutableArray<AnalyzerFileReference>> ChooseAnalyzersAsync(string configurationScopeId) =>
        await enterpriseAnalyzerProvider.GetEnterpriseOrNullAsync(configurationScopeId) ?? await basicAnalyzerProvider.GetBasicAsync();

    private async Task UpdateAnalyzersIfChangedAsync(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        logger.LogVerbose(new MessageLevelContext { VerboseContext = ["To Update"] }, PrintAnalyzersChoice(analyzersToUse));
        if (!DidAnalyzerChoiceChange(analyzersToUse))
        {
            logger.LogVerbose(new MessageLevelContext { VerboseContext = ["No Update"] }, "Nothing to update");
            return;
        }

        logger.LogVerbose(new MessageLevelContext { VerboseContext = ["Before Update"] }, roslynWorkspace.CurrentSolution?.DisplayCurrentAnalyzerState());
        var updatedSolution = await UpdateAnalyzersAsync(analyzersToUse);
        logger.LogVerbose(new MessageLevelContext { VerboseContext = ["After Update"] }, updatedSolution?.DisplayCurrentAnalyzerState());
    }

    private bool DidAnalyzerChoiceChange(ImmutableArray<AnalyzerFileReference> analyzersToUse) => !analyzerComparer.Equals(currentAnalyzers, analyzersToUse);

    private async Task RemoveCurrentAnalyzersAsync()
    {
        if (!currentAnalyzers.HasValue || await roslynWorkspace.TryApplyChangesAsync(new AnalyzerChange(currentAnalyzers.Value, NoChange)) is not null)
        {
            currentAnalyzers = null;
            return;
        }

        logger.LogVerbose(Resources.RoslynAnalyzersNotRemoved);
    }

    private async Task<IRoslynSolutionWrapper> UpdateAnalyzersAsync(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        if (await roslynWorkspace.TryApplyChangesAsync(new AnalyzerChange(currentAnalyzers ?? NoChange, analyzersToUse)) is { } solution)
        {
            currentAnalyzers = analyzersToUse;
            return solution;
        }

        logger.WriteLine(Resources.RoslynAnalyzersNotUpdated);
        return null;
    }

    private static string PrintAnalyzersChoice(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Analyzer update. Registering the following analyzers:");
        foreach (var analyzer in analyzersToUse)
        {
            stringBuilder.AppendLine($"    {analyzer.DisplayInfo()}");
        }
        var messageFormat = stringBuilder.ToString();
        return messageFormat;
    }

    private void OnConfigurationScopeChanged(object sender, EventArgs e) => OnSolutionStateChangedAsync(activeConfigScopeTracker.Current?.Id).Forget();

    private void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e) => OnSolutionStateChangedAsync(e?.SolutionName).Forget();
}
