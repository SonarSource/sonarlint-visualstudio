﻿/*
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
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AnalysisListener : IAnalysisListener
{
    private readonly IEnumerable<Language> supportedLanguages;

    private readonly IAnalysisService analysisService;
    private readonly IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter;
    private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;

    [ImportingConstructor]
    public AnalysisListener(IAnalysisService analysisService, IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter, IAnalysisStatusNotifierFactory analysisStatusNotifierFactory)
       : this(analysisService, raiseIssueParamsToAnalysisIssueConverter, analysisStatusNotifierFactory, new List<Language>() { Language.Secrets })
    {
    }

    internal AnalysisListener(IAnalysisService analysisService, IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter, IAnalysisStatusNotifierFactory analysisStatusNotifierFactory, IEnumerable<Language> supportedLanguages)
    {
        this.supportedLanguages = supportedLanguages;
        this.analysisService = analysisService;
        this.raiseIssueParamsToAnalysisIssueConverter = raiseIssueParamsToAnalysisIssueConverter;
        this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;
    }

    public Task DidChangeAnalysisReadinessAsync(DidChangeAnalysisReadinessParams parameters)
    {
        return Task.CompletedTask;
    }

    public void RaiseIssues(RaiseIssuesParams parameters)
    {
        if (parameters.analysisId.HasValue)
        {
            var filepath = parameters.issuesByFileUri.Single().Key.LocalPath;

            var issues = GetSupportedLanguageIssues(raiseIssueParamsToAnalysisIssueConverter.GetAnalysisIssues(parameters));

            if (issues.Any())
            {
                analysisService.PublishIssues(filepath, parameters.analysisId.Value, issues);
            }
            if (!parameters.isIntermediatePublication)
            {
                var analysisStatusNotifier = analysisStatusNotifierFactory.Create("SLCoreAnalyzer", filepath);
                analysisStatusNotifier.AnalysisFinished(issues.Count(), TimeSpan.Zero);
            }
        }
    }

    private IEnumerable<IAnalysisIssue> GetSupportedLanguageIssues(IEnumerable<IAnalysisIssue> issues)
    {
        return issues.Where(i => IsSupportedLanguage(i.RuleKey));
    }

    private bool IsSupportedLanguage(string ruleKey)
    {
        return supportedLanguages.Any(l => ruleKey.StartsWith(Language.GetSonarRepoKeyFromLanguage(l)));
    }

    public void RaiseHotspots(RaiseHotspotsParams parameters)
    {
        // no-op: We don't have hotspots in Secrets
        // it will be implemented when we support a language that have hotspots
    }
}
