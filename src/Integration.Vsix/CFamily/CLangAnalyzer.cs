/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal class CLangAnalyzer : IAnalyzer
    {
        private readonly ILogger logger;

        public CLangAnalyzer(ILogger logger)
        {
            this.logger = logger;
        }

        public bool IsAnalysisSupported(IEnumerable<SonarLanguage> languages)
        {
            return languages.Contains(SonarLanguage.CFamily);
        }

        public void RequestAnalysis(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
        {
            Debug.Assert(IsAnalysisSupported(detectedLanguages));

            ThreadHelper.ThrowIfNotOnUIThread();

            var request = CFamilyHelper.CreateRequest(logger, projectItem, path);
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
            // that are only available in newer versions of VS e.g. [Import] IThreadHandling.            // current versions.

            // Switch a background thread
            await TaskScheduler.Default;

            // W're tying up a background thread waiting for out-of-process analysis. We could
            // change the process runner so it works asynchronously. Alternatively, we could change the
            // RequestAnalysis method to be synchronous, rather than fire-and-forget.
            var response = CFamilyHelper.CallClangAnalyzer(request, new ProcessRunner(logger), logger);

            if (response != null)
            {
                var issues = response.Messages.Where(m => m.Filename == request.File)
                        .Select(m => CFamilyHelper.ToSonarLintIssue(m, request.CFamilyLanguage, RulesMetadataCache.Instance))
                        .ToList();

                // Switch back to the UI thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Note: the file being analyzed might have been closed by the time the analysis results are 
                // returned. This doesn't cause a crash; all active taggers will have been detached from the
                // TextBufferIssueTracker when the file was closed, but the TextBufferIssueTracker will
                // still exist and handle the call.
                consumer.Accept(request.File, issues);
            }
        }

    }
}
