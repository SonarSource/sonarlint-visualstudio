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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

public interface ISolutionRoslynAnalyzerManager :  IDisposable
{
    void OnSolutionChanged(string solutionName, BindingConfiguration bindingConfiguration);
}

[System.ComponentModel.Composition.Export(typeof(ISolutionRoslynAnalyzerManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SolutionRoslynAnalyzerManager : ISolutionRoslynAnalyzerManager
{
    private readonly IConnectedModeRoslynAnalyzerProvider connectedModeAnalyzerProvider;
    private readonly IEmbeddedRoslynAnalyzerProvider embeddedAnalyzerProvider;
    private readonly object lockObject = new();
    private readonly VisualStudioWorkspace roslynWorkspace;
    private readonly IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer;
    private ImmutableArray<AnalyzerFileReference>? currentAnalyzers;

    private SolutionInfo? currentState;

    [System.Composition.ImportingConstructor]
    public SolutionRoslynAnalyzerManager(IEmbeddedRoslynAnalyzerProvider embeddedAnalyzerProvider,
        IConnectedModeRoslynAnalyzerProvider connectedModeAnalyzerProvider,
        VisualStudioWorkspace roslynWorkspace)
        :this(embeddedAnalyzerProvider, connectedModeAnalyzerProvider, roslynWorkspace, AnalyzerArrayComparer.Instance)
    {
    }

    internal SolutionRoslynAnalyzerManager(IEmbeddedRoslynAnalyzerProvider embeddedAnalyzerProvider,
        IConnectedModeRoslynAnalyzerProvider connectedModeAnalyzerProvider,
        VisualStudioWorkspace roslynWorkspace,
        IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer)
    {
        this.embeddedAnalyzerProvider = embeddedAnalyzerProvider;
        this.connectedModeAnalyzerProvider = connectedModeAnalyzerProvider;
        this.roslynWorkspace = roslynWorkspace;
        this.analyzerComparer = analyzerComparer;

        connectedModeAnalyzerProvider.AnalyzerUpdatedForConnection += HandleConnectedModeAnalyzerUpdate;
    }

    public void OnSolutionChanged(string solutionName, BindingConfiguration bindingConfiguration)
    {
        lock (lockObject)
        {
            if (solutionName is null)
            {
                currentState = null;
                currentAnalyzers = null;
                return;
            }

            UpdateCurrentSolutionInfo(solutionName, bindingConfiguration, out var isSameSolution);

            var analyzersToUse = ChooseAnalyzers(bindingConfiguration.Project?.ServerConnection);

            if (isSameSolution)
            {
                if (analyzerComparer.Equals(currentAnalyzers, analyzersToUse))
                {
                    return;
                }

                RemoveCurrentAnalyzers();
            }

            ApplyAnalyzer(analyzersToUse);
        }
    }

    internal /* for testing */ void HandleConnectedModeAnalyzerUpdate(object sender, AnalyzerUpdatedForConnectionEventArgs args)
    {
        lock (lockObject)
        {
            if (args.Connection is null || currentState?.BindingConfiguration.Project?.ServerConnection != args.Connection)
            {
                return;
            }

            var analyzersToUse = ChooseAnalyzers(args.Connection);

            if (analyzerComparer.Equals(currentAnalyzers, analyzersToUse))
            {
                return;
            }

            RemoveCurrentAnalyzers();
            ApplyAnalyzer(analyzersToUse);
        }
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
            currentState = new SolutionInfo(solutionName, bindingConfiguration);
            isSameSolution = false;
        }
    }

    private ImmutableArray<AnalyzerFileReference> ChooseAnalyzers(ServerConnection serverConnection) =>
        ChooseAnalyzers(serverConnection is not null ? connectedModeAnalyzerProvider.GetOrNull(serverConnection) : null);

    private ImmutableArray<AnalyzerFileReference> ChooseAnalyzers(ImmutableArray<AnalyzerFileReference>? connectedModeAnalyzers) =>
        connectedModeAnalyzers ?? embeddedAnalyzerProvider.Get();

    private void RemoveCurrentAnalyzers()
    {
        if (currentAnalyzers == null)
        {
            return;
        }

        var updatedSolution = currentAnalyzers!
            .Value
            .Aggregate<AnalyzerFileReference, Solution>(
                roslynWorkspace.CurrentSolution, 
                (solution, analyzer) => 
                    solution.AnalyzerReferences.Contains(analyzer) 
                        ? solution.RemoveAnalyzerReference(analyzer) 
                        : solution);


        if (!roslynWorkspace.TryApplyChanges(updatedSolution))
        {
            throw new NotImplementedException();
        }

        currentAnalyzers = null;
    }

    private void ApplyAnalyzer(ImmutableArray<AnalyzerFileReference>? embeddedAnalyzers)
    {
        if (embeddedAnalyzers is null)
        {
            return;
        }

        if (!roslynWorkspace.TryApplyChanges(roslynWorkspace.CurrentSolution.WithAnalyzerReferences(embeddedAnalyzers)))
        {
            throw new NotImplementedException();
        }

        currentAnalyzers = embeddedAnalyzers;
    }

    private record struct SolutionInfo(string SolutionName, BindingConfiguration BindingConfiguration);

    public void Dispose()
    {
        connectedModeAnalyzerProvider.AnalyzerUpdatedForConnection -= HandleConnectedModeAnalyzerUpdate;
    }
}

interface IRoslynWorkspaceWrapper
{
    IRoslynSolutionWrapper CurrentSolution { get; }
    bool TryApplyChanges(IRoslynSolutionWrapper solution);
}

interface IRoslynSolutionWrapper
{
    bool ContainsAnalyzer(AnalyzerFileReference analyzerReference);
    IRoslynSolutionWrapper RemoveAnalyzerReference(AnalyzerFileReference analyzerReference);
    IRoslynSolutionWrapper WithAnalyzers(ImmutableArray<AnalyzerFileReference> analyzers);
    Solution GetRoslynSolution();
}

internal class AnalyzerArrayComparer : IEqualityComparer<ImmutableArray<AnalyzerFileReference>?>
{
    public static AnalyzerArrayComparer Instance { get; } = new();
    
    private AnalyzerArrayComparer()
    {
    }
    
    public bool Equals(ImmutableArray<AnalyzerFileReference>? x, ImmutableArray<AnalyzerFileReference>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Value.SequenceEqual(y.Value);
    }

    public int GetHashCode(ImmutableArray<AnalyzerFileReference>? obj) => obj.GetHashCode();
}
