/*
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
    Task OnSolutionBindingChangedAsync(string solutionName, BindingConfiguration bindingConfiguration);
}

[Export(typeof(ISolutionRoslynAnalyzerManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SolutionRoslynAnalyzerManager : ISolutionRoslynAnalyzerManager
{
    private bool disposed;
    private readonly IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer;
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
        ILogger logger) 
        : this(embeddedAnalyzerProvider, connectedModeAnalyzerProvider, roslynWorkspace, AnalyzerArrayComparer.Instance, logger)
    {
    }

    internal /* for testing */ SolutionRoslynAnalyzerManager(
        IEmbeddedRoslynAnalyzerProvider embeddedAnalyzerProvider,
        IConnectedModeRoslynAnalyzerProvider connectedModeAnalyzerProvider,
        IRoslynWorkspaceWrapper roslynWorkspace,
        IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer,
        ILogger logger)
    {
        this.embeddedAnalyzerProvider = embeddedAnalyzerProvider;
        this.connectedModeAnalyzerProvider = connectedModeAnalyzerProvider;
        this.roslynWorkspace = roslynWorkspace;
        this.analyzerComparer = analyzerComparer;
        this.logger = logger;

        connectedModeAnalyzerProvider.AnalyzerUpdatedForConnection += HandleConnectedModeAnalyzerUpdate;
    }

    public async Task OnSolutionBindingChangedAsync(string solutionName, BindingConfiguration bindingConfiguration)
    {
        await UpdateAnalyzersAsync(solutionName, bindingConfiguration);
    }

    private async Task UpdateAnalyzersAsync(string solutionName, BindingConfiguration bindingConfiguration)
    {
        var analyzersToUse = await ChooseAnalyzersAsync(bindingConfiguration.Project?.ServerConnection);

        lock (lockObject)
        {
            ThrowIfDisposed();
            
            if (solutionName is null)
            {
                currentState = null;
                currentAnalyzers = null;
                return;
            }

            UpdateCurrentSolutionInfo(solutionName, bindingConfiguration, out var isSameSolution);

            if (isSameSolution)
            {
                UpdateAnalyzersIfChanged(analyzersToUse);
            }
            else
            {
                AddAnalyzer(analyzersToUse);
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
        connectedModeAnalyzerProvider.AnalyzerUpdatedForConnection -= HandleConnectedModeAnalyzerUpdate;
        disposed = true;
    }

    internal /* for testing */ async Task HandleConnectedModeAnalyzerUpdateAsync(AnalyzerUpdatedForConnectionEventArgs args)
    {
        var analyzersToUse = await ChooseAnalyzersAsync(args.Connection);
        lock (lockObject)
        {
            ThrowIfDisposed();

            if (args.Connection is not null && currentState?.BindingConfiguration.Project?.ServerConnection.Id == args.Connection.Id)
            {
                UpdateAnalyzersIfChanged(analyzersToUse);
            }
        }
    }

    private void HandleConnectedModeAnalyzerUpdate(object sender, AnalyzerUpdatedForConnectionEventArgs args)
    {
        HandleConnectedModeAnalyzerUpdateAsync(args).Forget();
    }

    private void UpdateCurrentSolutionInfo(string solutionName, BindingConfiguration bindingConfiguration, out bool isSameSolution)
    {
        if (currentState is { SolutionName: var currentSolutionName } && solutionName == currentSolutionName)
        {
            currentState = currentState.Value with { BindingConfiguration = bindingConfiguration };
            isSameSolution = true;
        }
        else
        {
            currentState = new SolutionStateInfo(solutionName, bindingConfiguration);
            isSameSolution = false;
        }
    }

    private async Task<ImmutableArray<AnalyzerFileReference>> ChooseAnalyzersAsync(ServerConnection serverConnection) =>
        ChooseAnalyzers(serverConnection is not null ? (await connectedModeAnalyzerProvider.GetOrNullAsync()) : null);

    private ImmutableArray<AnalyzerFileReference> ChooseAnalyzers(ImmutableArray<AnalyzerFileReference>? connectedModeAnalyzers) =>
        connectedModeAnalyzers ?? embeddedAnalyzerProvider.Get();
    
    private void UpdateAnalyzersIfChanged(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        if (!DidAnalyzerChoiceChange(analyzersToUse))
        {
            return;
        }

        UpdateAnalyzers(analyzersToUse);
    }

    private bool DidAnalyzerChoiceChange(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        Debug.Assert(currentAnalyzers is not null);
        return !analyzerComparer.Equals(currentAnalyzers, analyzersToUse);
    }

    private void UpdateAnalyzers(ImmutableArray<AnalyzerFileReference> analyzersToUse)
    {
        if (!roslynWorkspace.TryApplyChanges(roslynWorkspace.CurrentSolution.RemoveAnalyzerReferences(currentAnalyzers!.Value)))
        {
            const string message = "Failed to remove analyzer references while updating analyzers";
            Debug.Assert(true, message);
            logger.LogVerbose(message);
            throw new NotImplementedException();
        }

        currentAnalyzers = null;
        
        AddAnalyzer(analyzersToUse);
    }

    private void AddAnalyzer(ImmutableArray<AnalyzerFileReference> analyzerToUse)
    {
        if (!roslynWorkspace.TryApplyChanges(roslynWorkspace.CurrentSolution.WithAnalyzerReferences(analyzerToUse)))
        {
            const string message = "Failed to add analyzer references while adding analyzers";
            Debug.Assert(true, message);
            logger.LogVerbose(message);
            throw new NotImplementedException();
        }

        currentAnalyzers = analyzerToUse;
    }

    private record struct SolutionStateInfo(string SolutionName, BindingConfiguration BindingConfiguration);
}
