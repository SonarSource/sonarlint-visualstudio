/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    [Export(typeof(IAnalyzer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CLangAnalyzer : IAnalyzer
    {
        private readonly ITelemetryManager telemetryManager;
        private readonly ISonarLintSettings settings;
        private readonly ICFamilyRulesConfigProvider cFamilyRulesConfigProvider;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CLangAnalyzer(ITelemetryManager telemetryManager, ISonarLintSettings settings, ICFamilyRulesConfigProvider cFamilyRulesConfigProvider, ILogger logger)
        {
            this.telemetryManager = telemetryManager;
            this.settings = settings;
            this.cFamilyRulesConfigProvider = cFamilyRulesConfigProvider;
            this.logger = logger;
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return languages.Contains(AnalysisLanguage.CFamily);
        }

        public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
        {
            Debug.Assert(IsAnalysisSupported(detectedLanguages));

            var request = CFamilyHelper.CreateRequest(logger, projectItem, path, cFamilyRulesConfigProvider);
            if (request == null)
            {
                return;
            }

            TriggerAnalysisAsync(request, consumer)
                .Forget(); // fire and forget
        }

        private async Task TriggerAnalysisAsync(Request request, IIssueConsumer consumer)
        {
            // For notes on VS threading, see https://github.com/microsoft/vs-threading/blob/master/doc/cookbook_vs.md
            // Note: we support multiple versions of VS which prevents us from using some threading helper methods
            // that are only available in newer versions of VS e.g. [Import] IThreadHandling.          

            // Switch a background thread
            await TaskScheduler.Default;

            logger.WriteLine($"Analyzing {request.File}");

            // We're tying up a background thread waiting for out-of-process analysis. We could
            // change the process runner so it works asynchronously. Alternatively, we could change the
            // RequestAnalysis method to be synchronous, rather than fire-and-forget.
            var response = CFamilyHelper.CallClangAnalyzer(request, new ProcessRunner(settings, logger), logger);

            if (response != null)
            {
                Debug.Assert(response.Messages.All(m => m.Filename == request.File), "Issue for unexpected file returned");

                var issues = response.Messages
                        .Where(m => IsIssueForActiveRule(m, request.RulesConfiguration))
                        .Select(m => CFamilyHelper.ToSonarLintIssue(m, request.CFamilyLanguage, request.RulesConfiguration))
                        .ToList();

                telemetryManager.LanguageAnalyzed(request.CFamilyLanguage); // different keys for C and C++

                logger.WriteLine($"Found {issues.Count} issue(s)");

                // Switch back to the UI thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Note: the file being analyzed might have been closed by the time the analysis results are 
                // returned. This doesn't cause a crash; all active taggers will have been detached from the
                // TextBufferIssueTracker when the file was closed, but the TextBufferIssueTracker will
                // still exist and handle the call.
                consumer.Accept(request.File, issues);
            }
        }

        internal /* for testing */ static bool IsIssueForActiveRule(Message message, ICFamilyRulesConfig rulesConfiguration)
        {
            // Currently (v6.3) the subprocess.exe will always run the native CLang rules, so those issues
            // could be returned even if they were not activated in the profile.

            // In addition, in v6.4+ there are internal rules that are always enabled and will always return
            // issues. Filtering for active rules will also remove those internal issues since the corresponding
            // rules will never be active in a quality profile.
            return rulesConfiguration.ActivePartialRuleKeys.Contains(message.RuleKey, CFamilyShared.RuleKeyComparer);
        }
    }
}
