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
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AnalysisListener : IAnalysisListener
{
    private readonly Language[] supportedLanguages;

    private readonly IAnalysisService analysisService;
    private readonly IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter;
    private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;

    [ImportingConstructor]
    public AnalysisListener(IAnalysisService analysisService, IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter, IAnalysisStatusNotifierFactory analysisStatusNotifierFactory)
       : this(analysisService, raiseIssueParamsToAnalysisIssueConverter, analysisStatusNotifierFactory, [Language.Secrets])
    {
    }

    internal AnalysisListener(IAnalysisService analysisService, IRaiseIssueParamsToAnalysisIssueConverter raiseIssueParamsToAnalysisIssueConverter, IAnalysisStatusNotifierFactory analysisStatusNotifierFactory, Language[] supportedLanguages)
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
        if (!parameters.analysisId.HasValue || parameters.isIntermediatePublication)
        {
            return;
        }

        var fileUri = parameters.issuesByFileUri.Single().Key;
        var raisedIssues = parameters.issuesByFileUri.Single().Value;

        var supportedRaisedIssues = GetSupportedLanguageIssues(raisedIssues);

        if (supportedRaisedIssues.Any())
        {
            analysisService.PublishIssues(fileUri.LocalPath,
               parameters.analysisId.Value,
               raiseIssueParamsToAnalysisIssueConverter.GetAnalysisIssues(fileUri, supportedRaisedIssues));

            var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), fileUri.LocalPath);
            analysisStatusNotifier.AnalysisFinished(supportedRaisedIssues.Length, TimeSpan.Zero);
        }
    }

    private RaisedIssueDto[] GetSupportedLanguageIssues(IEnumerable<RaisedIssueDto> issues) =>
        issues.Where(i => IsSupportedLanguage(i.ruleKey)).ToArray();

    private bool IsSupportedLanguage(string ruleKey) =>
        Array.Exists(supportedLanguages, l => ruleKey.StartsWith(Language.GetSonarRepoKeyFromLanguage(l)));

    public void RaiseHotspots(RaiseHotspotsParams parameters)
    {
        // no-op: We don't have hotspots in Secrets
        // it will be implemented when we support a language that have hotspots
    }
}
