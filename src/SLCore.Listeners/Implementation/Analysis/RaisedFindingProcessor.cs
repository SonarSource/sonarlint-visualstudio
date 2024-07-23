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
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;

internal interface IRaisedFindingProcessor
{
    void RaiseFinding<T>(RaiseFindingParams<T> parameters) where T : RaisedFindingDto;
}

[Export(typeof(IRaisedFindingProcessor))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class RaisedFindingProcessor : IRaisedFindingProcessor
{
    private readonly IAnalysisService analysisService;
    private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
    private readonly List<string> analyzableLanguagesRuleKeyPrefixes;
    private readonly ILogger logger;
    private readonly IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter;

    [ImportingConstructor]
    public RaisedFindingProcessor(ISLCoreConstantsProvider slCoreConstantsProvider,
        IAnalysisService analysisService,
        IRaiseFindingToAnalysisIssueConverter raiseFindingToAnalysisIssueConverter,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
        ILogger logger)
    {
        this.analysisService = analysisService;
        this.raiseFindingToAnalysisIssueConverter = raiseFindingToAnalysisIssueConverter;
        this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;
        this.logger = logger;

        analyzableLanguagesRuleKeyPrefixes = CalculateAnalyzableRulePrefixes(slCoreConstantsProvider);
    }
    
    public void RaiseFinding<T>(RaiseFindingParams<T> parameters) where T : RaisedFindingDto
    {
        if (!IsValid(parameters))
        {
            return;
        }

        PublishFindings(parameters);
    }
    
    private bool IsValid<T>(RaiseFindingParams<T> parameters) where T : RaisedFindingDto
    {
        var logContext = $"[{nameof(RaiseFinding)}+{typeof(T).Name}]";
        if (!parameters.analysisId.HasValue)
        {
            logger.LogVerbose($"{logContext} No {nameof(parameters.analysisId)}, ignoring...");
            return false;
        }

        if (parameters.isIntermediatePublication)
        {
            logger.LogVerbose($"{logContext} {nameof(parameters.isIntermediatePublication)}=true, ignoring...");
            return false;
        }

        if (parameters.issuesByFileUri.Count == 0)
        {
            logger.LogVerbose($"{logContext} Empty {nameof(parameters.issuesByFileUri)} dictionary, ignoring...");
            return false;
        }

        return true;
    }
    
    private void PublishFindings<T>(RaiseFindingParams<T> parameters) where T : RaisedFindingDto
    {
        foreach (var fileAndIssues in parameters.issuesByFileUri)
        {
            var fileUri = fileAndIssues.Key;
            var localPath = fileUri.LocalPath;
            var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), localPath, parameters.analysisId);
            var supportedRaisedIssues = GetSupportedLanguageFindings(fileAndIssues.Value ?? []);
            analysisService.PublishIssues(localPath,
                parameters.analysisId!.Value,
                raiseFindingToAnalysisIssueConverter.GetAnalysisIssues(fileUri, supportedRaisedIssues));
            analysisStatusNotifier.AnalysisFinished(supportedRaisedIssues.Length, TimeSpan.Zero);
        }
    }

    private T[] GetSupportedLanguageFindings<T>(IEnumerable<T> findings) where T : RaisedFindingDto
    {
        return findings.Where(i => analyzableLanguagesRuleKeyPrefixes.Exists(languageRepo => i.ruleKey.StartsWith(languageRepo))).ToArray();
    }
    
    private static List<string> CalculateAnalyzableRulePrefixes(ISLCoreConstantsProvider slCoreConstantsProvider)
    {
        return slCoreConstantsProvider.SLCoreAnalyzableLanguages?
            .Select(x => x.ConvertToCoreLanguage())
            .Select(Language.GetSonarRepoKeyFromLanguage)
            .Where(r => r is not null)
            .ToList() ?? [];
    }
}
