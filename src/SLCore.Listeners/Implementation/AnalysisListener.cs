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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.State;
using AnalyzerOptions = SonarLint.VisualStudio.Core.Analysis.AnalyzerOptions;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AnalysisListener : IAnalysisListener
{
    private readonly ISLCoreConstantsProvider slCoreConstantsProvider;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IAnalysisRequester analysisRequester;
    private readonly IAnalysisService analysisService;
    private readonly IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter;
    private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
    private readonly ILogger logger;

    [ImportingConstructor]
    public AnalysisListener(ISLCoreConstantsProvider slCoreConstantsProvider, IActiveConfigScopeTracker activeConfigScopeTracker,
        IAnalysisRequester analysisRequester,
        IAnalysisService analysisService,
        IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
        ILogger logger)
    {
        this.slCoreConstantsProvider = slCoreConstantsProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.analysisRequester = analysisRequester;
        this.analysisService = analysisService;
        this.raiseIssueParamsToAnalysisIssueConverter = raiseIssueParamsToAnalysisIssueConverter;
        this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;
        this.logger = logger;
    }

    public void DidChangeAnalysisReadiness(DidChangeAnalysisReadinessParams parameters)
    {
        var configScopeId = parameters.configurationScopeIds.Single();

        if (activeConfigScopeTracker.TryUpdateAnalysisReadinessOnCurrentConfigScope(configScopeId, parameters.areReadyForAnalysis))
        {
            logger.WriteLine(SLCoreStrings.AnalysisReadinessUpdate, parameters.areReadyForAnalysis);
            if (parameters.areReadyForAnalysis)
            {
                analysisRequester.RequestAnalysis(new AnalyzerOptions{ IsOnOpen = true });
            }
        }
        else
        {
            logger.WriteLine(SLCoreStrings.AnalysisReadinessUpdate, SLCoreStrings.ConfigScopeConflict);   
        }
    }

    public void RaiseIssues(RaiseIssuesParams parameters)
    {
        if (!parameters.analysisId.HasValue)
        {
            logger.LogVerbose($"[{nameof(RaiseIssues)}] No {nameof(parameters.analysisId)}, ignoring...");
            return;
        }

        if (parameters.isIntermediatePublication)
        {
            logger.LogVerbose($"[{nameof(RaiseIssues)}] {nameof(parameters.isIntermediatePublication)}=true, ignoring...");
            return;
        }

        if (parameters.issuesByFileUri.Count == 0)
        {
            logger.LogVerbose($"[{nameof(RaiseIssues)}] Empty {nameof(parameters.issuesByFileUri)} dictionary, ignoring...");
            return;
        }

        foreach (var fileAndIssues in parameters.issuesByFileUri)
        {
            var fileUri = fileAndIssues.Key;
            var localPath = fileUri.LocalPath;
            var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), localPath, parameters.analysisId);
            var supportedRaisedIssues = GetSupportedLanguageIssues(fileAndIssues.Value ?? Enumerable.Empty<RaisedIssueDto>());
            analysisService.PublishIssues(localPath,
                parameters.analysisId.Value,
                raiseIssueParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri, supportedRaisedIssues));
            analysisStatusNotifier.AnalysisFinished(supportedRaisedIssues.Length, TimeSpan.Zero);
        }
    }

    private RaisedIssueDto[] GetSupportedLanguageIssues(IEnumerable<RaisedIssueDto> issues)
    {
        var analyzableLanguages = slCoreConstantsProvider.AnalyzableLanguages
            .Select(x => x.ConvertToCoreLanguage())
            .Select(Language.GetSonarRepoKeyFromLanguage)
            .Where(r => r is not null)
            .ToArray();
        return issues.Where(i => analyzableLanguages.Any(languageRepo => i.ruleKey.StartsWith(languageRepo))).ToArray();
    }

    public void RaiseHotspots(RaiseHotspotsParams parameters)
    {
        // no-op: We don't have hotspots in Secrets
        // it will be implemented when we support a language that has hotspots
    }
}
