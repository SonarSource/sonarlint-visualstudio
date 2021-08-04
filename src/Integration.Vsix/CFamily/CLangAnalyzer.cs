﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Telemetry;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal interface ICFamilyAnalyzer : IAnalyzer
    {
        void ExecuteAnalysis(string path,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            IAnalysisStatusNotifier statusNotifier,
            CancellationToken cancellationToken);
    }

    [Export(typeof(IAnalyzer))]
    [Export(typeof(ICFamilyAnalyzer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CLangAnalyzer : ICFamilyAnalyzer
    {
        private readonly ITelemetryManager telemetryManager;
        private readonly ISonarLintSettings settings;
        private readonly ICFamilyRulesConfigProvider cFamilyRulesConfigProvider;
        private readonly IAnalysisStatusNotifier analysisStatusNotifier;
        private readonly ILogger logger;
        private readonly DTE dte;
        private readonly ICFamilyIssueToAnalysisIssueConverter issueConverter;

        [ImportingConstructor]
        public CLangAnalyzer(ITelemetryManager telemetryManager, ISonarLintSettings settings, ICFamilyRulesConfigProvider cFamilyRulesConfigProvider, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IAnalysisStatusNotifier analysisStatusNotifier, ILogger logger, ICFamilyIssueToAnalysisIssueConverter issueConverter)
        {
            this.telemetryManager = telemetryManager;
            this.settings = settings;
            this.cFamilyRulesConfigProvider = cFamilyRulesConfigProvider;
            this.analysisStatusNotifier = analysisStatusNotifier;
            this.logger = logger;
            this.issueConverter = issueConverter;
            this.dte = serviceProvider.GetService<DTE>();
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return languages.Contains(AnalysisLanguage.CFamily);
        }

        public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer, IAnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            ExecuteAnalysis(path, detectedLanguages, consumer, analyzerOptions, analysisStatusNotifier, cancellationToken);
        }

        public void ExecuteAnalysis(string path,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            IAnalysisStatusNotifier statusNotifier,
            CancellationToken cancellationToken)
        {
            var projectItem = dte?.Solution?.FindProjectItem(path);
            if (projectItem == null)
            {
                return;
            }

            Debug.Assert(IsAnalysisSupported(detectedLanguages));

            var request = CreateRequest(logger, projectItem, path, cFamilyRulesConfigProvider, analyzerOptions);
            if (request == null)
            {
                return;
            }

            TriggerAnalysis(request, consumer, statusNotifier, cancellationToken);
        }

        protected /* for testing */ virtual IRequest CreateRequest(ILogger logger, ProjectItem projectItem, string absoluteFilePath, ICFamilyRulesConfigProvider cFamilyRulesConfigProvider, IAnalyzerOptions analyzerOptions) =>
            CFamilyHelper.CreateRequest(logger, projectItem, absoluteFilePath, cFamilyRulesConfigProvider, analyzerOptions);

        protected /* for testing */ virtual void TriggerAnalysis(IRequest request, IIssueConsumer consumer, IAnalysisStatusNotifier statusNotifier, CancellationToken cancellationToken) =>
            TriggerAnalysisAsync(request, consumer, statusNotifier, cancellationToken)
                .Forget(); // fire and forget

        protected /* for testing */ virtual void CallSubProcess(Action<Message> handleMessage, IRequest request, ISonarLintSettings settings, ILogger logger, CancellationToken cancellationToken)
        {
            CFamilyHelper.CallClangAnalyzer(handleMessage, request, new ProcessRunner(settings, logger), logger, cancellationToken);
        }

        internal /* for testing */ async Task TriggerAnalysisAsync(IRequest request, IIssueConsumer consumer, IAnalysisStatusNotifier statusNotifier, CancellationToken cancellationToken)
        {
            // For notes on VS threading, see https://github.com/microsoft/vs-threading/blob/master/doc/cookbook_vs.md
            // Note: we support multiple versions of VS which prevents us from using some threading helper methods
            // that are only available in newer versions of VS e.g. [Import] IThreadHandling.          

            // Switch to a background thread
            await TaskScheduler.Default;

            var analysisStartTime = DateTime.Now;
            statusNotifier?.AnalysisStarted(request.File);
            int issueCount = 0;

            var handleMessage = consumer == null
                ? (Action<Message>) (message => { })
                : message => HandleMessage(message, request, consumer, ref issueCount);

            try
            {
                // We're tying up a background thread waiting for out-of-process analysis. We could
                // change the process runner so it works asynchronously. Alternatively, we could change the
                // RequestAnalysis method to be asynchronous, rather than fire-and-forget.
                CallSubProcess(handleMessage, request, settings, logger, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    statusNotifier?.AnalysisCancelled(request.File);
                }
                else
                {
                    var analysisTime = DateTime.Now - analysisStartTime;
                    statusNotifier?.AnalysisFinished(request.File, issueCount, analysisTime);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                statusNotifier?.AnalysisFailed(request.File, ex);
            }

            telemetryManager.LanguageAnalyzed(request.CFamilyLanguage); // different keys for C and C++
        }

        private void HandleMessage(Message message, IRequest request, IIssueConsumer consumer, ref int issueCount)
        {
            Debug.Assert(message.Filename == request.File, $"Issue for unexpected file returned: {message.Filename}");
            if (!IsIssueForActiveRule(message, request.RulesConfiguration))
            {
                return;
            }

            issueCount++;
            var issue = issueConverter.Convert(message, request.CFamilyLanguage, request.RulesConfiguration);

            // Note: the file being analyzed might have been closed by the time the analysis results are 
            // returned. This doesn't cause a crash; all active taggers will have been detached from the
            // TextBufferIssueTracker when the file was closed, but the TextBufferIssueTracker will
            // still exist and handle the call.
            consumer.Accept(request.File, new[] { issue });
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
