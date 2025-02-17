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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ETW;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;

/// <summary>
/// Responsible for listening to <see cref="IServerIssuesStore.ServerIssuesChanged" /> and calling
/// <see cref="IRoslynSettingsFileStorage.Update" /> with the new suppressions.
/// </summary>
public interface IRoslynSettingsFileSynchronizer : IDisposable
{
}

[Export(typeof(IRoslynSettingsFileSynchronizer))]
[PartCreationPolicy(CreationPolicy.NonShared)] // stateless - doesn't need to be shared
internal sealed class RoslynSettingsFileSynchronizer : IRoslynSettingsFileSynchronizer
{
    private readonly IConfigurationProvider configurationProvider;
    private readonly ISuppressedIssuesCalculatorFactory suppressedIssuesCalculatorFactory;
    private readonly IRoslynSettingsFileStorage roslynSettingsFileStorage;
    private readonly ISolutionInfoProvider solutionInfoProvider;
    private readonly ISolutionBindingRepository solutionBindingRepository;
    private readonly ISuppressionUpdater suppressionUpdater;
    private readonly IThreadHandling threadHandling;
    private readonly object lockObject = new();

    [ImportingConstructor]
    public RoslynSettingsFileSynchronizer(
        IRoslynSettingsFileStorage roslynSettingsFileStorage,
        IConfigurationProvider configurationProvider,
        ISolutionInfoProvider solutionInfoProvider,
        ISolutionBindingRepository solutionBindingRepository,
        ISuppressionUpdater suppressionUpdater,
        ISuppressedIssuesCalculatorFactory suppressedIssuesCalculatorFactory)
        : this(roslynSettingsFileStorage,
            configurationProvider,
            solutionInfoProvider,
            solutionBindingRepository,
            suppressionUpdater,
            suppressedIssuesCalculatorFactory,
            ThreadHandling.Instance)
    {
    }

    internal RoslynSettingsFileSynchronizer(
        IRoslynSettingsFileStorage roslynSettingsFileStorage,
        IConfigurationProvider configurationProvider,
        ISolutionInfoProvider solutionInfoProvider,
        ISolutionBindingRepository solutionBindingRepository,
        ISuppressionUpdater suppressionUpdater,
        ISuppressedIssuesCalculatorFactory suppressedIssuesCalculatorFactory,
        IThreadHandling threadHandling)
    {
        this.roslynSettingsFileStorage = roslynSettingsFileStorage;
        this.configurationProvider = configurationProvider;
        this.solutionInfoProvider = solutionInfoProvider;
        this.solutionBindingRepository = solutionBindingRepository;
        this.suppressionUpdater = suppressionUpdater;
        this.suppressedIssuesCalculatorFactory = suppressedIssuesCalculatorFactory;
        this.threadHandling = threadHandling;

        this.suppressionUpdater.SuppressedIssuesReloaded += OnSuppressedIssuesReloaded;
        this.suppressionUpdater.NewIssuesSuppressed += OnNewIssuesSuppressed;
        this.suppressionUpdater.SuppressionsRemoved += OnSuppressionsRemoved;
        solutionBindingRepository.BindingDeleted += OnBindingDeleted;
    }

    private void OnBindingDeleted(object sender, LocalBindingKeyEventArgs e) => roslynSettingsFileStorage.Delete(e.LocalBindingKey);

    public void Dispose()
    {
        solutionBindingRepository.BindingDeleted -= OnBindingDeleted;
        suppressionUpdater.SuppressedIssuesReloaded -= OnSuppressedIssuesReloaded;
        suppressionUpdater.NewIssuesSuppressed -= OnNewIssuesSuppressed;
        suppressionUpdater.SuppressionsRemoved -= OnSuppressionsRemoved;
    }

    private void OnSuppressedIssuesReloaded(object sender, SuppressionsEventArgs e) =>
        UpdateFileStorageAsync(suppressedIssuesCalculatorFactory.CreateAllSuppressedIssuesCalculator(e.SuppressedIssues)).Forget();

    private void OnNewIssuesSuppressed(object sender, SuppressionsEventArgs e)
    {
        if (!e.SuppressedIssues.Any())
        {
            return;
        }

        UpdateFileStorageAsync(suppressedIssuesCalculatorFactory.CreateNewSuppressedIssuesCalculator(e.SuppressedIssues)).Forget();
    }

    private void OnSuppressionsRemoved(object sender, SuppressionsRemovedEventArgs e)
    {
        if (!e.IssueServerKeys.Any())
        {
            return;
        }

        UpdateFileStorageAsync(suppressedIssuesCalculatorFactory.CreateSuppressedIssuesRemovedCalculator(e.IssueServerKeys)).Forget();
    }

    /// <summary>
    /// Updates the Roslyn suppressed issues file if in connected mode
    /// </summary>
    private async Task UpdateFileStorageAsync(ISuppressedIssuesCalculator suppressedIssuesCalculator) =>
        await threadHandling.RunOnBackgroundThread(async () =>
        {
            await UpdateFileStorageIfNeededAsync(suppressedIssuesCalculator);
            return true;
        });

    private async Task UpdateFileStorageIfNeededAsync(ISuppressedIssuesCalculator suppressedIssuesCalculator)
    {
        CodeMarkers.Instance.FileSynchronizerUpdateStart();
        try
        {
            var solutionNameWithoutExtension = await solutionInfoProvider.GetSolutionNameAsync();
            if (string.IsNullOrEmpty(solutionNameWithoutExtension))
            {
                return;
            }

            var sonarProjectKey = configurationProvider.GetConfiguration().Project?.ServerProjectKey;
            if (string.IsNullOrEmpty(sonarProjectKey))
            {
                SafeDeleteRoslynSettingsFileStorage(solutionNameWithoutExtension);
                return;
            }

            SafeUpdateRoslynSettingsFileStorage(suppressedIssuesCalculator, solutionNameWithoutExtension, sonarProjectKey);
        }
        finally
        {
            CodeMarkers.Instance.FileSynchronizerUpdateStop();
        }
    }

    private void SafeDeleteRoslynSettingsFileStorage(string solutionNameWithoutExtension)
    {
        lock (lockObject)
        {
            roslynSettingsFileStorage.Delete(solutionNameWithoutExtension);
        }
    }

    private void SafeUpdateRoslynSettingsFileStorage(
        ISuppressedIssuesCalculator suppressedIssuesCalculator,
        string solutionNameWithoutExtension,
        string sonarProjectKey)
    {
        lock (lockObject)
        {
            var suppressionsToAdd = suppressedIssuesCalculator.GetSuppressedIssuesOrNull(solutionNameWithoutExtension);
            if (suppressionsToAdd == null)
            {
                return;
            }
            var roslynSettings = new RoslynSettings { SonarProjectKey = sonarProjectKey, Suppressions = suppressionsToAdd };
            roslynSettingsFileStorage.Update(roslynSettings, solutionNameWithoutExtension);
        }
    }
}
