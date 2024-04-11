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
    private readonly IIssueSelectionService issueSelectionService;
    private readonly IIssueDetailDtoToAnalysisIssueConverter dtoToIssueConverter;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IAnalysisIssueVisualizationConverter issueToVisualizationConverter;
    private readonly ILogger logger;
    
    public OpenIssueInIdeHandler(IIDEWindowService ideWindowService,
        ILocationNavigator navigator,
        IIssueSelectionService issueSelectionService,
        IIssueDetailDtoToAnalysisIssueConverter dtoToIssueConverter,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IAnalysisIssueVisualizationConverter issueToVisualizationConverter,
        ILogger logger)
    {
        this.ideWindowService = ideWindowService;
        this.navigator = navigator;
        this.issueSelectionService = issueSelectionService;
        this.dtoToIssueConverter = dtoToIssueConverter;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.issueToVisualizationConverter = issueToVisualizationConverter;
        this.logger = logger;
    }

    public void ShowIssue(IssueDetailDto issueDetails, string configurationScope)
    {
        ideWindowService.BringToFront();
        
        if (issueDetails.isTaint)
        {
            throw new NotImplementedException();
        }

        var configScope = activeConfigScopeTracker.Current;

        if (configScope.Id != configurationScope)
        {
            throw new NotImplementedException();
        }

        if (configScope.SonarProjectId == null)
        {
            throw new NotImplementedException();
        }

        if (configScope.RootPath == null)
        {
            throw new NotImplementedException();
        }

        var visualization = issueToVisualizationConverter.Convert(dtoToIssueConverter.Convert(issueDetails, configScope.RootPath));
        
        if (!navigator.TryNavigate(visualization))
        {
            throw new NotImplementedException();
        }

        issueSelectionService.SelectedIssue = visualization;
    }
}
