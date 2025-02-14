﻿/*
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
using System.IO;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ETW;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarQube.Client.Models;

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
    private readonly ILogger logger;
    private readonly IRoslynSettingsFileStorage roslynSettingsFileStorage;
    private readonly ISolutionInfoProvider solutionInfoProvider;
    private readonly ISolutionBindingRepository solutionBindingRepository;
    private readonly IRoslynSuppressionUpdater roslynSuppressionUpdater;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public RoslynSettingsFileSynchronizer(
        IServerIssuesStore serverIssuesStore,
        IRoslynSettingsFileStorage roslynSettingsFileStorage,
        IConfigurationProvider configurationProvider,
        ISolutionInfoProvider solutionInfoProvider,
        ISolutionBindingRepository solutionBindingRepository,
        IRoslynSuppressionUpdater roslynSuppressionUpdater,
        ILogger logger)
        : this(roslynSettingsFileStorage,
            configurationProvider,
            solutionInfoProvider,
            solutionBindingRepository,
            roslynSuppressionUpdater,
            logger,
            ThreadHandling.Instance)
    {
    }

    internal RoslynSettingsFileSynchronizer(
        IRoslynSettingsFileStorage roslynSettingsFileStorage,
        IConfigurationProvider configurationProvider,
        ISolutionInfoProvider solutionInfoProvider,
        ISolutionBindingRepository solutionBindingRepository,
        IRoslynSuppressionUpdater roslynSuppressionUpdater,
        ILogger logger,
        IThreadHandling threadHandling)
    {
        this.roslynSettingsFileStorage = roslynSettingsFileStorage;
        this.configurationProvider = configurationProvider;
        this.solutionInfoProvider = solutionInfoProvider;
        this.solutionBindingRepository = solutionBindingRepository;
        this.roslynSuppressionUpdater = roslynSuppressionUpdater;
        this.logger = logger;
        this.threadHandling = threadHandling;

        this.roslynSuppressionUpdater.SuppressedIssuesReloaded += OnSuppressedIssuesReloaded;
        this.roslynSuppressionUpdater.NewIssuesSuppressed += OnNewIssuesSuppressed;
        this.roslynSuppressionUpdater.SuppressionsRemoved += OnSuppressionsRemoved;
        solutionBindingRepository.BindingDeleted += OnBindingDeleted;
    }

    private void OnBindingDeleted(object sender, LocalBindingKeyEventArgs e) => roslynSettingsFileStorage.Delete(e.LocalBindingKey);

    public void Dispose()
    {
        solutionBindingRepository.BindingDeleted -= OnBindingDeleted;
        roslynSuppressionUpdater.SuppressedIssuesReloaded -= OnSuppressedIssuesReloaded;
        roslynSuppressionUpdater.NewIssuesSuppressed -= OnNewIssuesSuppressed;
        roslynSuppressionUpdater.SuppressionsRemoved -= OnSuppressionsRemoved;
    }

    private async Task<string> GetSolutionNameWithoutExtensionAsync()
    {
        var fullSolutionFilePath = await solutionInfoProvider.GetFullSolutionFilePathAsync();
        return Path.GetFileNameWithoutExtension(fullSolutionFilePath);
    }

    private void OnSuppressedIssuesReloaded(object sender, SuppressionsEventArgs e)
    {
        IEnumerable<SuppressedIssue> GetSuppressionsToAdd(string settingsKey) => GetRoslynSuppressedIssues(e.SuppressedIssues);

        UpdateFileStorageAsync(GetSuppressionsToAdd).Forget();
    }

    private static SuppressedIssue[] GetRoslynSuppressedIssues(IEnumerable<SonarQubeIssue> sonarQubeIssues)
    {
        var suppressionsToAdd = sonarQubeIssues
            .Where(x => x.IsResolved)
            .Select(IssueConverter.Convert)
            .Where(x => x.RoslynLanguage != RoslynLanguage.Unknown && !string.IsNullOrEmpty(x.RoslynRuleId))
            .ToArray();
        return suppressionsToAdd;
    }

    private void OnNewIssuesSuppressed(object sender, SuppressionsEventArgs e)
    {
        if (!e.SuppressedIssues.Any())
        {
            return;
        }

        IEnumerable<SuppressedIssue> GetMergedSuppressedIssues(string settingsKey)
        {
            var suppressedIssuesToAdd = GetRoslynSuppressedIssues(e.SuppressedIssues);
            var suppressedIssuesInFile = roslynSettingsFileStorage.Get(settingsKey)?.Suppressions;
            if (suppressedIssuesInFile is null)
            {
                // if the settings do not exist on disk, add all the new suppressed issues
                return suppressedIssuesToAdd;
            }

            var suppressedIssuesToAddNotExistingInFile = suppressedIssuesToAdd.Where(newIssue => suppressedIssuesInFile.All(existing => !newIssue.AreSame(existing)));
            return suppressedIssuesInFile.Concat(suppressedIssuesToAddNotExistingInFile);
        }

        UpdateFileStorageAsync(GetMergedSuppressedIssues).Forget();
    }

    private void OnSuppressionsRemoved(object sender, SuppressionsRemovedEventArgs e)
    {
        if (!e.IssueServerKeys.Any())
        {
            return;
        }

        IEnumerable<SuppressedIssue> GetSuppressedIssuesAfterRemoved(string settingsKey)
        {
            var suppressedIssuesInFile = roslynSettingsFileStorage.Get(settingsKey)?.Suppressions?.ToList();
            var resolvedIssues = suppressedIssuesInFile?.Where(existingIssue => e.IssueServerKeys.Any(x => existingIssue.IssueServerKey == x)).ToList();
            if (resolvedIssues == null || !resolvedIssues.Any())
            {
                // nothing to be done if no issue from file was resolved
                return null;
            }
            return suppressedIssuesInFile.Except(resolvedIssues);
        }

        UpdateFileStorageAsync(GetSuppressedIssuesAfterRemoved).Forget();
    }

    /// <summary>
    /// Updates the Roslyn suppressed issues file if in connected mode
    /// </summary>
    /// <remarks>The method will switch to a background if required, and will *not* return to the UI thread on completion.</remarks>
    private async Task UpdateFileStorageAsync(Func<string, IEnumerable<SuppressedIssue>> getSuppressedIssuesOrNull)
    {
        CodeMarkers.Instance.FileSynchronizerUpdateStart();
        try
        {
            await threadHandling.SwitchToBackgroundThread();

            var solutionNameWithoutExtension = await GetSolutionNameWithoutExtensionAsync();
            if (string.IsNullOrEmpty(solutionNameWithoutExtension))
            {
                return;
            }

            var sonarProjectKey = configurationProvider.GetConfiguration().Project?.ServerProjectKey;
            if (string.IsNullOrEmpty(sonarProjectKey))
            {
                roslynSettingsFileStorage.Delete(solutionNameWithoutExtension);
                return;
            }

            var suppressionsToAdd = getSuppressedIssuesOrNull(solutionNameWithoutExtension);
            if (suppressionsToAdd == null)
            {
                return;
            }

            var roslynSettings = new RoslynSettings { SonarProjectKey = sonarProjectKey, Suppressions = suppressionsToAdd };
            roslynSettingsFileStorage.Update(roslynSettings, solutionNameWithoutExtension);
        }
        finally
        {
            CodeMarkers.Instance.FileSynchronizerUpdateStop();
        }
    }

    // Converts SonarQube issues to SuppressedIssues that can be compared more easily with Roslyn issues
    internal static class IssueConverter
    {
        public static SuppressedIssue Convert(SonarQubeIssue issue)
        {
            var (repoKey, ruleKey) = GetRepoAndRuleKey(issue.RuleId);
            var language = GetRoslynLanguage(repoKey);

            var line = issue.TextRange == null ? (int?)null : issue.TextRange.StartLine - 1;
            return new SuppressedIssue
            {
                RoslynRuleId = ruleKey,
                FilePath = issue.FilePath,
                Hash = issue.Hash,
                RoslynLanguage = language,
                RoslynIssueLine = line,
                IssueServerKey = issue.IssueKey
            };
        }

        private static (string repoKey, string ruleKey) GetRepoAndRuleKey(string sonarRuleId)
        {
            // Sonar rule ids are in the form "[repo key]:[rule key]"
            var separatorPos = sonarRuleId.IndexOf(":", StringComparison.OrdinalIgnoreCase);
            if (separatorPos > -1)
            {
                var repoKey = sonarRuleId.Substring(0, separatorPos);
                var ruleKey = sonarRuleId.Substring(separatorPos + 1);

                return (repoKey, ruleKey);
            }

            return (null, null); // invalid rule key -> ignore
        }

        private static RoslynLanguage GetRoslynLanguage(string repoKey)
        {
            // Currently the only Sonar repos which contain Roslyn analysis rules are
            // csharpsquid and vbnet. These include "normal" and "hotspot" rules.
            // The taint rules are in a different repo, and the part that is implemented
            // as a Roslyn analyzer won't raise issues anyway.
            switch (repoKey)
            {
                case "csharpsquid": // i.e. the rules in SonarAnalyzer.CSharp
                    return RoslynLanguage.CSharp;
                case "vbnet": // i.e. SonarAnalyzer.VisualBasic
                    return RoslynLanguage.VB;
                default:
                    return RoslynLanguage.Unknown;
            }
        }
    }
}
