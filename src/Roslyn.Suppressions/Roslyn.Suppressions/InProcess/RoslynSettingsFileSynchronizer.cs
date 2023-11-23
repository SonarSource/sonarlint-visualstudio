/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Synchronization;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ETW;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.InProcess
{
    /// <summary>
    /// Responsible for listening to <see cref="IServerIssuesStore.ServerIssuesChanged"/> and calling
    /// <see cref="IRoslynSettingsFileStorage.Update"/> with the new suppressions.
    /// </summary>
    public interface IRoslynSettingsFileSynchronizer : IDisposable
    {
        Task UpdateFileStorageAsync();
    }

    [Export(typeof(IRoslynSettingsFileSynchronizer))]
    [PartCreationPolicy(CreationPolicy.NonShared)] // stateless - doesn't need to be shared
    internal sealed class RoslynSettingsFileSynchronizer : IRoslynSettingsFileSynchronizer
    {
        private readonly IThreadHandling threadHandling;
        private readonly IServerIssuesStore serverIssuesStore;
        private readonly IRoslynSettingsFileStorage roslynSettingsFileStorage;
        private readonly IConfigurationProvider configurationProvider;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly ILogger logger;

        [ImportingConstructor]
        public RoslynSettingsFileSynchronizer(IServerIssuesStore serverIssuesStore,
            IRoslynSettingsFileStorage roslynSettingsFileStorage,
            IConfigurationProvider configurationProvider,
            ISolutionInfoProvider solutionInfoProvider,
            ILogger logger)
            : this(serverIssuesStore,
                roslynSettingsFileStorage,
                configurationProvider,
                solutionInfoProvider,
                logger,
                ThreadHandling.Instance)
        {
        }

        internal RoslynSettingsFileSynchronizer(IServerIssuesStore serverIssuesStore,
            IRoslynSettingsFileStorage roslynSettingsFileStorage,
            IConfigurationProvider configurationProvider,
            ISolutionInfoProvider solutionInfoProvider,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.serverIssuesStore = serverIssuesStore;
            this.roslynSettingsFileStorage = roslynSettingsFileStorage;
            this.configurationProvider = configurationProvider;
            this.solutionInfoProvider = solutionInfoProvider;
            this.logger = logger;
            this.threadHandling = threadHandling;

            serverIssuesStore.ServerIssuesChanged += OnServerIssuesChanged;
        }

        private void OnServerIssuesChanged(object sender, EventArgs e)
        {
            // Called on the UI thread, so unhandled exceptions will crash VS.
            // Note: we don't expect any exceptions to be thrown, since the called method
            // does all of its work on a background thread.
            try
            {                
                UpdateFileStorageAsync().Forget();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Squash non-critical exceptions
                logger.LogVerbose(ex.ToString());
            }
        }

        /// <summary>
        /// Updates the Roslyn suppressed issues file if in connected mode
        /// </summary>
        /// <remarks>The method will switch to a background if required, and will *not*
        /// return to the UI thread on completion.</remarks>
        public async Task UpdateFileStorageAsync()
        {
            CodeMarkers.Instance.FileSynchronizerUpdateStart();
            try
            {
                await threadHandling.SwitchToBackgroundThread();

                var sonarProjectKey = configurationProvider.GetConfiguration().Project?.ProjectKey;

                if (!string.IsNullOrEmpty(sonarProjectKey))
                {
                    var fullSolutionFilePath = await solutionInfoProvider.GetFullSolutionFilePathAsync();
                    Debug.Assert(fullSolutionFilePath != null, "Not expecting the solution name to be null in Connected Mode");
                    var solnNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fullSolutionFilePath);

                    var allSuppressedIssues = serverIssuesStore.Get().Where(x => x.IsResolved);

                    var settings = new RoslynSettings
                    {
                        SonarProjectKey = sonarProjectKey,
                        Suppressions = allSuppressedIssues
                                            .Select(x => IssueConverter.Convert(x))
                                            .Where(x => x.RoslynLanguage != RoslynLanguage.Unknown && !string.IsNullOrEmpty(x.RoslynRuleId))
                                            .ToArray(),
                    };
                    roslynSettingsFileStorage.Update(settings, solnNameWithoutExtension);
                }
            }
            finally
            {
                CodeMarkers.Instance.FileSynchronizerUpdateStop();
            }
        }

        public void Dispose()
        {
            serverIssuesStore.ServerIssuesChanged -= OnServerIssuesChanged;
        }

        // Converts SonarQube issues to SuppressedIssues that can be compared more easily with Roslyn issues
        internal static class IssueConverter
        {
            public static SuppressedIssue Convert(SonarQubeIssue issue)
            {
                (string repoKey, string ruleKey) = GetRepoAndRuleKey(issue.RuleId);
                var language = GetRoslynLanguage(repoKey);

                int? line = issue.TextRange == null ? (int?)null : issue.TextRange.StartLine - 1;
                return new SuppressedIssue
                {
                    RoslynRuleId = ruleKey,
                    FilePath = issue.FilePath,
                    Hash = issue.Hash,
                    RoslynLanguage = language,
                    RoslynIssueLine = line
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
                    case "vbnet":       // i.e. SonarAnalyzer.VisualBasic
                        return RoslynLanguage.VB;
                    default:
                        return RoslynLanguage.Unknown;
                }
            }
        }
    }
}
