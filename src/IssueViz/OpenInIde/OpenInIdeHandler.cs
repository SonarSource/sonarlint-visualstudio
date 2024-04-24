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
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

public interface IOpenInIdeHandler
{
    void ShowIssue<T>(T issueDetails,
        string configurationScope,
        IOpenInIdeConverter<T> converter,
        Guid toolWindowId,
        IOpenInIdeVisualizationProcessor visualizationProcessor = null) where T : IIssueDetail;
}

[Export(typeof(IOpenInIdeHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenInIdeHandler : IOpenInIdeHandler
{
    private readonly IEducation education;
    private readonly IIDEWindowService ideWindowService;
    private readonly IOpenInIdeFailureInfoBar infoBarManager;
    private readonly IIssueSelectionService issueSelectionService;
    private readonly ILogger logger;
    private readonly ILocationNavigator navigator;
    private readonly IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator;
    private readonly IThreadHandling thereHandling;

    [ImportingConstructor]
    public OpenInIdeHandler(IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        IOpenInIdeFailureInfoBar infoBarManager,
        IIDEWindowService ideWindowService,
        ILocationNavigator navigator,
        IIssueSelectionService issueSelectionService,
        IEducation education,
        ILogger logger,
        IThreadHandling thereHandling)
    {
        this.ideWindowService = ideWindowService;
        this.navigator = navigator;
        this.education = education;
        this.infoBarManager = infoBarManager;
        this.issueSelectionService = issueSelectionService;
        this.openInIdeConfigScopeValidator = openInIdeConfigScopeValidator;
        this.logger = logger;
        this.thereHandling = thereHandling;
    }

    public void ShowIssue<T>(T issueDetails,
        string configurationScope,
        IOpenInIdeConverter<T> converter,
        Guid toolWindowId,
        IOpenInIdeVisualizationProcessor visualizationProcessor = null) where T : IIssueDetail
    {
        thereHandling.RunOnBackgroundThread(async () =>
        {
            await ShowIssueInternalAsync(issueDetails, configurationScope, converter, toolWindowId, visualizationProcessor);
            return 0;
        }).Forget();
    }

    private async Task ShowIssueInternalAsync<T>(T issueDetails, string issueConfigurationScope, IOpenInIdeConverter<T> converter, Guid toolWindowId, IOpenInIdeVisualizationProcessor visualizationProcessor) where T : IIssueDetail
    {
        logger.WriteLine(OpenInIdeResources.ProcessingRequest, issueConfigurationScope,
            issueDetails.Key, issueDetails.Type);

        ideWindowService.BringToFront();

        if (!openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(issueConfigurationScope, out var configurationScopeRoot)
            || !converter.TryConvert(issueDetails, configurationScopeRoot, out var visualization))
        {
            await infoBarManager.ShowAsync(toolWindowId);
            return;
        }

        visualizationProcessor?.HandleConvertedIssue(visualization);

        if (!TryShowIssue(visualization))
        {
            await infoBarManager.ShowAsync(toolWindowId);
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

        logger.WriteLine(OpenInIdeResources.IssueLocationNotFound, visualization.Location.FilePath,
            visualization.Location?.TextRange?.StartLine, visualization.Location?.TextRange?.StartLineOffset);
        return false;
    }
}
