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
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIDE;

public interface IOpenIssueInIdeHandler
{
    void ShowIssue(IssueDetailDto issueDetails, string configurationScope);
}

[Export(typeof(IOpenIssueInIdeHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenIssueInIdeHandler : IOpenIssueInIdeHandler
{
    private readonly IIDEWindowService ideWindowService;
    private readonly ILocationNavigator navigator;
    private readonly IEducation education;
    private readonly IOpenInIDEFailureInfoBar infoBarManager;
    private readonly IIssueSelectionService issueSelectionService;
    private readonly IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator;
    private readonly IOpenInIdeConverter openInIdeConverter;
    private readonly ILogger logger;
    private readonly IThreadHandling thereHandling;

    [ImportingConstructor]
    public OpenIssueInIdeHandler(IIDEWindowService ideWindowService,
        ILocationNavigator navigator,
        IEducation education,
        IOpenInIDEFailureInfoBar infoBarManager,
        IIssueSelectionService issueSelectionService,
        IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        IOpenInIdeConverter openInIdeConverter,
        ILogger logger,
        IThreadHandling thereHandling)
    {
        this.ideWindowService = ideWindowService;
        this.navigator = navigator;
        this.education = education;
        this.infoBarManager = infoBarManager;
        this.issueSelectionService = issueSelectionService;
        this.openInIdeConfigScopeValidator = openInIdeConfigScopeValidator;
        this.openInIdeConverter = openInIdeConverter;
        this.logger = logger;
        this.thereHandling = thereHandling;
    }

    public void ShowIssue(IssueDetailDto issueDetails, string configurationScope)
    {
        thereHandling.RunOnBackgroundThread(async () =>
        {
            await ShowIssueInternalAsync(issueDetails, configurationScope);
            return 0;
        }).Forget();
    }

    private async Task ShowIssueInternalAsync(IssueDetailDto issueDetails, string issueConfigurationScope)
    {
        logger.WriteLine(OpenInIDEResources.ApiHandler_ProcessingIssueRequest, issueConfigurationScope, issueDetails.issueKey);
        
        ideWindowService.BringToFront();

        if (!openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(issueConfigurationScope, out var configurationScopeRoot)
            || !openInIdeConverter.TryConvertIssue(issueDetails, configurationScopeRoot, out var visualization)
            || !TryShowIssue(visualization))
        {
            await ShowInfoBarAsync(issueDetails.isTaint);
            return;
        }

        issueSelectionService.SelectedIssue = visualization;
        await infoBarManager.ClearAsync();

        if (SonarCompositeRuleId.TryParse(visualization.Issue.RuleKey, out var ruleId))
        {
            education.ShowRuleHelp(ruleId, visualization.Issue.RuleDescriptionContextKey);
        }
    }

    private bool TryShowIssue(IAnalysisIssueVisualization visualization)
    {
        if (navigator.TryNavigate(visualization))
        {
            return true;
        }

        logger.WriteLine("todo");
        return false;
    }

    private async Task ShowInfoBarAsync(bool isTaint)
    {
        await infoBarManager.ShowAsync(isTaint ? IssueListIds.TaintId : IssueListIds.ErrorListId);
    }
}
