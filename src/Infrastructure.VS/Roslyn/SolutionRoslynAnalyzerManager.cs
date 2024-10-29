﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

public interface ISolutionRoslynAnalyzerManager : IDisposable
{

}

[Export(typeof(ISolutionRoslynAnalyzerManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SolutionRoslynAnalyzerManager : ISolutionRoslynAnalyzerManager
{
    private bool disposed;
    private readonly IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IActiveSolutionTracker activeSolutionBoundTracker;
    private readonly IThreadHandling threadHandling;
    private readonly IConnectedModeRoslynAnalyzerProvider connectedModeAnalyzerProvider;
    private readonly IEmbeddedRoslynAnalyzerProvider embeddedAnalyzerProvider;
    private readonly ILogger logger;
    private readonly object lockObject = new();
    private readonly IRoslynWorkspaceWrapper roslynWorkspace;
    private ImmutableArray<AnalyzerFileReference>? currentAnalyzers;

    private SolutionStateInfo? currentState;

    [ImportingConstructor]
    public SolutionRoslynAnalyzerManager(
        IEmbeddedRoslynAnalyzerProvider embeddedAnalyzerProvider,
        IConnectedModeRoslynAnalyzerProvider connectedModeAnalyzerProvider,
        IRoslynWorkspaceWrapper roslynWorkspace,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IActiveSolutionTracker activeSolutionBoundTracker,
        ILogger logger) 
        : this(embeddedAnalyzerProvider, connectedModeAnalyzerProvider, roslynWorkspace, AnalyzerArrayComparer.Instance, activeConfigScopeTracker, activeSolutionBoundTracker, new ThreadHandling(), logger)
    {
    }

    internal /* for testing */ SolutionRoslynAnalyzerManager(
        IEmbeddedRoslynAnalyzerProvider embeddedAnalyzerProvider,
        IConnectedModeRoslynAnalyzerProvider connectedModeAnalyzerProvider,
        IRoslynWorkspaceWrapper roslynWorkspace,
        IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IActiveSolutionTracker activeSolutionBoundTracker,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.embeddedAnalyzerProvider = embeddedAnalyzerProvider;
        this.connectedModeAnalyzerProvider = connectedModeAnalyzerProvider;
        this.roslynWorkspace = roslynWorkspace;
        this.analyzerComparer = analyzerComparer;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.threadHandling = threadHandling;
        this.logger = logger;


        activeConfigScopeTracker.CurrentConfigurationScopeChanged += OnCurrentConfigScopeChanged;
        activeSolutionBoundTracker.ActiveSolutionChanged += OnActiveSolutionChanged;
    }

    private void OnCurrentConfigScopeChanged(object sender, ConfigurationScope e)
    {
        OnSolutionStateChangedAsync(e?.Id).Forget();
    }

    private void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
    {
        OnSolutionStateChangedAsync(e.SolutionName).Forget();
    }

    private async Task OnSolutionStateChangedAsync(string solutionName)
    {
        lock (lockObject)
        {
            ThrowIfDisposed();
            
            if (solutionName is null)
            {
                currentState = null;
                RemoveCurrentAnalyzers();
                return;
            }

            UpdateCurrentSolutionInfo(solutionName);
        }

        var analyzersToUse = await ChooseAnalyzersAsync();
        UpdateAnalyzersIfChanged(analyzersToUse);
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
        activeConfigScopeTracker.CurrentConfigurationScopeChanged -= OnCurrentConfigScopeChanged;
        activeSolutionBoundTracker.ActiveSolutionChanged -= OnActiveSolutionChanged;
        disposed = true;
    }

    private void UpdateCurrentSolutionInfo(string solutionName)
    {
        if (currentState is not { SolutionName: var currentSolutionName } || solutionName != currentSolutionName)
        {
            currentState = new SolutionStateInfo(solutionName);
        }
    }

    private async Task<ImmutableArray<AnalyzerFileReference>> ChooseAnalyzersAsync() =>
        ChooseAnalyzers(await connectedModeAnalyzerProvider.GetOrNullAsync());

    private ImmutableArray<AnalyzerFileReference> ChooseAnalyzers(ImmutableArray<AnalyzerFileReference>? connectedModeAnalyzers) =>
        connectedModeAnalyzers ?? embeddedAnalyzerProvider.Get();
    
    private void UpdateAnalyzersIfChanged(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        if (!DidAnalyzerChoiceChange(analyzersToUse))
        {
            return;
        }

        RemoveCurrentAnalyzers();
        AddAnalyzer(analyzersToUse);
    }

    private bool DidAnalyzerChoiceChange(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        return !analyzerComparer.Equals(currentAnalyzers, analyzersToUse);
    }

    private void RemoveCurrentAnalyzers()
    {
        threadHandling.RunOnUIThread(() =>
        {
            if (currentAnalyzers.HasValue && !roslynWorkspace.TryApplyChanges(roslynWorkspace.CurrentSolution.RemoveAnalyzerReferences(currentAnalyzers.Value)))
            {
                const string message = "Failed to remove analyzer references while updating analyzers";
                Debug.Assert(true, message);
                logger.LogVerbose(message);
                throw new NotImplementedException();
            }

            currentAnalyzers = null;
        });
    }

    private void AddAnalyzer(ImmutableArray<AnalyzerFileReference> analyzerToUse)
    {
        threadHandling.RunOnUIThread(() =>
        {
            if (!roslynWorkspace.TryApplyChanges(roslynWorkspace.CurrentSolution.AddAnalyzerReferences(analyzerToUse)))
            {
                const string message = "Failed to add analyzer references while adding analyzers";
                Debug.Assert(true, message);
                logger.LogVerbose(message);
                throw new NotImplementedException();
            }

            currentAnalyzers = analyzerToUse;
        });
    }

    private record struct SolutionStateInfo(string SolutionName);
}