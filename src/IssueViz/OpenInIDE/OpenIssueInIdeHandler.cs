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
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
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
    private static readonly Guid ErrorListToolWindowId = new Guid(ToolWindowGuids80.ErrorList);

    private readonly IIDEWindowService ideWindowService;
    private readonly ILocationNavigator navigator;
    private readonly IEducation education;
    private readonly IOpenInIDEFailureInfoBar infoBarManager;
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
        IOpenInIDEFailureInfoBar infoBarManager,
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
        this.infoBarManager = infoBarManager;
        this.issueSelectionService = issueSelectionService;
        this.dtoToIssueConverter = dtoToIssueConverter;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.issueToVisualizationConverter = issueToVisualizationConverter;
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

        if (issueDetails.isTaint)
        {
            logger.WriteLine(OpenInIDEResources.ApiHandler_TaintIssuesNotSupported);
            return;
        }

        var configurationScopeRoot = await ValidateConfigurationScopeAsync(issueConfigurationScope);

        if (!ConvertIssue(issueDetails, configurationScopeRoot, out var visualization))
        {
            await infoBarManager.ShowAsync(ErrorListToolWindowId);
        }

        if (!navigator.TryNavigate(visualization))
        {
            logger.WriteLine("todo");
        }

        issueSelectionService.SelectedIssue = visualization;

        if (SonarCompositeRuleId.TryParse(visualization.Issue.RuleKey, out var ruleId))
        {
            education.ShowRuleHelp(ruleId, visualization.Issue.RuleDescriptionContextKey);
        }
    }

    private async Task<string> ValidateConfigurationScopeAsync(string issueConfigurationScope)
    {
        var configScope = activeConfigScopeTracker.Current;

        if (configScope is null || configScope.Id != issueConfigurationScope)
        {
            logger.WriteLine(OpenInIDEResources.ApiHandler_ConfigurationScopeMismatch, configScope, issueConfigurationScope);
            await infoBarManager.ShowAsync(ErrorListToolWindowId);
            return null;
        }

        if (configScope?.SonarProjectId == null)
        {
            logger.WriteLine("todo");
            await infoBarManager.ShowAsync(ErrorListToolWindowId);
            return null;
        }

        if (string.IsNullOrEmpty(configScope?.RootPath))
        {
            logger.WriteLine("todo");
            await infoBarManager.ShowAsync(ErrorListToolWindowId);
        }

        return configScope.RootPath;
    }

    private bool ConvertIssue(IssueDetailDto issueDetails, string rootPath,
        out IAnalysisIssueVisualization visualization)
    {
        visualization = null;

        try
        {
            var analysisIssueBase = dtoToIssueConverter.Convert(issueDetails, rootPath);
            visualization = issueToVisualizationConverter.Convert(analysisIssueBase);
            return true;
        }
        catch (Exception e) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(e))
        {
            logger.WriteLine(OpenInIDEResources.ApiHandler_UnableToConvertHotspotData, e.Message);
            return false;
        }
    }
}
