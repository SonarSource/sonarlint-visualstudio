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

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIDE;

public interface IOpenIssueInIdeHandler
{
    void ShowIssue(IssueDetailDto issueDetails, string configurationScope);
}

[Export(typeof(IOpenIssueInIdeHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class OpenIssueInIdeHandler : IOpenIssueInIdeHandler
{
    private readonly IIDEWindowService ideWindowService;
    private readonly ILocationNavigator navigator;
    private readonly IEducation education;
    private readonly IIssueSelectionService issueSelectionService;
    private readonly IIssueDetailDtoToAnalysisIssueConverter dtoToIssueConverter;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IAnalysisIssueVisualizationConverter issueToVisualizationConverter;
    private readonly ILogger logger;
    private readonly IThreadHandling thereHandling;

    [ImportingConstructor]
    public OpenIssueInIdeHandler(IIDEWindowService ideWindowService,
        ILocationNavigator navigator,
        IEducation education,
        IIssueSelectionService issueSelectionService,
        IIssueDetailDtoToAnalysisIssueConverter dtoToIssueConverter,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IAnalysisIssueVisualizationConverter issueToVisualizationConverter,
        ILogger logger,
        IThreadHandling thereHandling)
    {
        this.ideWindowService = ideWindowService;
        this.navigator = navigator;
        this.education = education;
        this.issueSelectionService = issueSelectionService;
        this.dtoToIssueConverter = dtoToIssueConverter;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.issueToVisualizationConverter = issueToVisualizationConverter;
        this.logger = logger;
        this.thereHandling = thereHandling;
    }

    public void ShowIssue(IssueDetailDto issueDetails, string configurationScope)
    {
        thereHandling.RunOnBackgroundThread(() =>
        {
            ShowIssueInternal(issueDetails, configurationScope);
            return Task.FromResult(0);
        }).Forget();
    }

    private void ShowIssueInternal(IssueDetailDto issueDetails, string configurationScope)
    {
        ideWindowService.BringToFront();

        if (issueDetails.isTaint)
        {
            // taints are not supported yet
            throw new NotImplementedException();
        }

        var configScope = activeConfigScopeTracker.Current;

        if (configScope.Id != configurationScope)
        {
            // config scope changed
            throw new NotImplementedException();
        }

        if (configScope.SonarProjectId == null)
        {
            // not in connected mode
            throw new NotImplementedException();
        }

        if (configScope.RootPath == null)
        {
            // no root
            throw new NotImplementedException();
        }

        var analysisIssueBase = dtoToIssueConverter.Convert(issueDetails, configScope.RootPath);
        var visualization = issueToVisualizationConverter.Convert(analysisIssueBase);

        if (!SonarCompositeRuleId.TryParse(analysisIssueBase.RuleKey, out var ruleId))
        {
            // invalid rule id
            throw new NotImplementedException();
        }

        if (!navigator.TryNavigate(visualization))
        {
            // location unavailable
            throw new NotImplementedException();
        }

        issueSelectionService.SelectedIssue = visualization;

        education.ShowRuleHelp(ruleId, analysisIssueBase.RuleDescriptionContextKey);
    }
}
