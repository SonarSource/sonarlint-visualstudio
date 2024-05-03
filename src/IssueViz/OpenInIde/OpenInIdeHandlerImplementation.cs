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

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

public interface IOpenInIdeHandlerImplementation
{
    void ShowIssue<T>(T issueDetails,
        string configurationScope,
        IOpenInIdeIssueToAnalysisIssueConverter<T> converter,
        Guid toolWindowId,
        IOpenInIdeVisualizationProcessor visualizationProcessor = null) where T : IOpenInIdeIssue;
}

[Export(typeof(IOpenInIdeHandlerImplementation))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenInIdeHandlerImplementation : IOpenInIdeHandlerImplementation
{
    private readonly IIDEWindowService ideWindowService;
    private readonly IToolWindowService toolWindowService;
    private readonly IOpenInIdeMessageBox messageBox;
    private readonly IIssueSelectionService issueSelectionService;
    private readonly ILogger logger;
    private readonly ILocationNavigator navigator;
    private readonly IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator;
    private readonly IOpenInIdeConverterImplementation converterImplementation;
    private readonly IThreadHandling thereHandling;

    [ImportingConstructor]
    public OpenInIdeHandlerImplementation(IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        IOpenInIdeConverterImplementation converterImplementation,
        IToolWindowService toolWindowService,
        IOpenInIdeMessageBox messageBox,
        IIDEWindowService ideWindowService,
        ILocationNavigator navigator,
        IIssueSelectionService issueSelectionService,
        ILogger logger,
        IThreadHandling thereHandling)
    {
        this.ideWindowService = ideWindowService;
        this.navigator = navigator;
        this.toolWindowService = toolWindowService;
        this.messageBox = messageBox;
        this.issueSelectionService = issueSelectionService;
        this.openInIdeConfigScopeValidator = openInIdeConfigScopeValidator;
        this.converterImplementation = converterImplementation;
        this.logger = logger;
        this.thereHandling = thereHandling;
    }

    public void ShowIssue<T>(T issueDetails,
        string configurationScope,
        IOpenInIdeIssueToAnalysisIssueConverter<T> converter,
        Guid toolWindowId,
        IOpenInIdeVisualizationProcessor visualizationProcessor = null) where T : IOpenInIdeIssue
    {
        thereHandling.RunOnBackgroundThread(() =>
        {
            ShowIssueInternal(issueDetails, configurationScope, converter, toolWindowId, visualizationProcessor);
            return Task.FromResult(0);
        }).Forget();
    }

    private void ShowIssueInternal<T>(T issueDetails, string issueConfigurationScope, IOpenInIdeIssueToAnalysisIssueConverter<T> converter, Guid toolWindowId, IOpenInIdeVisualizationProcessor visualizationProcessor) where T : IOpenInIdeIssue
    {
        logger.WriteLine(OpenInIdeResources.ProcessingRequest, issueConfigurationScope,
            issueDetails?.Key, issueDetails?.Type);

        ideWindowService.BringToFront();

        if (!ValidateIssueNotNull(issueDetails, out var failureReason)
            || !ValidateConfiguration(issueConfigurationScope, out var configurationScopeRoot, out failureReason)
            || !ValidateIssueIsConvertible(issueDetails, converter, configurationScopeRoot, out var visualization, out failureReason))
        {
            messageBox.InvalidRequest(failureReason);
            return;
        }

        if (visualizationProcessor is not null)
        {
            visualization = visualizationProcessor.HandleConvertedIssue(visualization) ;
        }
        issueSelectionService.SelectedIssue = visualization;

        toolWindowService.Show(toolWindowId);

        var navigationResult = navigator.TryNavigatePartial(visualization);
        if (navigationResult != NavigationResult.OpenedLocation)
        {
            HandleIncompleteNavigation(visualization, navigationResult);
        }
        else
        {
            logger.WriteLine(string.Format(OpenInIdeResources.DoneProcessingRequest, issueDetails?.Key));
        }
    }

    private bool ValidateIssueNotNull<T>(T issueDetails, out string failureReason) where T : IOpenInIdeIssue
    {
        failureReason = default;

        if (issueDetails is { Key: not null })
        {
            return true;
        }

        failureReason = OpenInIdeResources.ValidationReason_MalformedRequest;
        return false;

    }

    private bool ValidateConfiguration(string issueConfigurationScope, out string configurationScopeRoot, out string failureReason)
    {
        return openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(issueConfigurationScope, out configurationScopeRoot, out failureReason);
    }

    private bool ValidateIssueIsConvertible<T>(T issueDetails,
        IOpenInIdeIssueToAnalysisIssueConverter<T> converter,
        string configurationScopeRoot,
        out IAnalysisIssueVisualization visualization,
        out string failureReason) where T : IOpenInIdeIssue
    {
        failureReason = default;

        if (converterImplementation.TryConvert(issueDetails, configurationScopeRoot, converter, out visualization))
        {
            return true;
        }

        failureReason = OpenInIdeResources.ValidationReason_UnableToConvertIssue;
        return false;
    }

    private void HandleIncompleteNavigation(IAnalysisIssueVisualization visualization, NavigationResult navigationResult)
    {
        logger.WriteLine(OpenInIdeResources.IssueLocationNotFound, visualization.CurrentFilePath,
            visualization.Location?.TextRange?.StartLine, visualization.Location?.TextRange?.StartLineOffset);
        
        switch (navigationResult)
        {
            case NavigationResult.Failed:
                messageBox.UnableToOpenFile(visualization.CurrentFilePath);
                return;
            case NavigationResult.OpenedFile:
                messageBox.UnableToLocateIssue(visualization.CurrentFilePath);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(navigationResult), OpenInIdeResources.Exception_InvalidNavigationResult);
        }
    }
}
