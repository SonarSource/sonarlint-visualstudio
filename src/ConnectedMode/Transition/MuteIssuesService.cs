﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Transition;

[Export(typeof(IMuteIssuesService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class MuteIssuesService(
    IMuteIssuesWindowService muteIssuesWindowService,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    ISLCoreServiceProvider slCoreServiceProvider,
    IServerIssueFinder serverIssueFinder,
    IRoslynSuppressionUpdater roslynSuppressionUpdater,
    ILogger logger,
    IThreadHandling threadHandling)
    : IMuteIssuesService
{
    private readonly ILogger logger = logger.ForContext(nameof(MuteIssuesService));

    public async Task ResolveIssueWithDialogAsync(IFilterableIssue issue)
    {
        threadHandling.ThrowIfOnUIThread();
        var issueServerKey = await GetIssueServerKeyAsync(issue);
        var currentConfigScope = activeConfigScopeTracker.Current;
        CheckIsInConnectedMode(currentConfigScope);
        CheckIssueServerKeyNotNullOrEmpty(issueServerKey);

        var allowedStatuses = await GetAllowedStatusesAsync(currentConfigScope.ConnectionId, issueServerKey);
        var windowResponse = await PromptMuteIssueResolutionAsync(allowedStatuses);
        await MuteIssueAsync(currentConfigScope.Id, issueServerKey, issue, windowResponse.IssueTransition.Value);
        await AddCommentAsync(currentConfigScope.Id, issueServerKey, windowResponse.Comment);
    }

    private async Task<string> GetIssueServerKeyAsync(IFilterableIssue issue)
    {
        // Non-Roslyn issues already have the issue server key
        if (issue is IAnalysisIssueVisualization issueViz)
        {
            return issueViz.Issue.IssueServerKey;
        }

        // Roslyn issues need to be converted to SonarQube issues to get the server key as they are handled by SLCore
        var serverIssue = await serverIssueFinder.FindServerIssueAsync(issue, CancellationToken.None);
        if (serverIssue is { IsResolved: true })
        {
            logger.WriteLine(Resources.MuteIssue_ErrorIssueAlreadyResolved);
            throw new MuteIssueException(Resources.MuteIssue_ErrorIssueAlreadyResolved);
        }
        return serverIssue?.IssueKey;
    }

    private async Task<MuteIssuesWindowResponse> PromptMuteIssueResolutionAsync(IEnumerable<ResolutionStatus> allowedStatuses)
    {
        MuteIssuesWindowResponse windowResponse = null;
        var allowedTransitions = allowedStatuses.Select(s => s.ToSonarQubeIssueTransition());
        await threadHandling.RunOnUIThreadAsync(() => windowResponse = muteIssuesWindowService.Show(allowedTransitions));

        if (windowResponse.Result)
        {
            return windowResponse;
        }

        throw new MuteIssueException.MuteIssueCancelledException();
    }

    private void CheckIssueServerKeyNotNullOrEmpty(string issueServerKey)
    {
        if (issueServerKey is { Length: > 0 })
        {
            return;
        }

        logger.WriteLine(Resources.MuteIssue_IssueNotFound);
        throw new MuteIssueException(Resources.MuteIssue_IssueNotFound);
    }

    private void CheckIsInConnectedMode(Core.ConfigurationScope.ConfigurationScope currentConfigScope)
    {
        if (currentConfigScope is { Id: not null, ConnectionId: not null })
        {
            return;
        }

        logger.WriteLine(Resources.MuteIssue_NotInConnectedMode);
        throw new MuteIssueException(Resources.MuteIssue_NotInConnectedMode);
    }

    private IIssueSLCoreService GetIssueSlCoreService()
    {
        if (slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService issueSlCoreService))
        {
            return issueSlCoreService;
        }

        logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
        throw new MuteIssueException(SLCoreStrings.ServiceProviderNotInitialized);
    }

    private async Task<List<ResolutionStatus>> GetAllowedStatusesAsync(string connectionId, string issueServerKey)
    {
        CheckStatusChangePermittedResponse response;
        try
        {
            var issueSlCoreService = GetIssueSlCoreService();
            var checkStatusChangePermittedParams = new CheckStatusChangePermittedParams(connectionId, issueServerKey);
            response = await issueSlCoreService.CheckStatusChangePermittedAsync(checkStatusChangePermittedParams);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.MuteIssue_AnErrorOccurred, issueServerKey, ex.Message);
            throw new MuteIssueException(ex);
        }

        if (!response.permitted)
        {
            logger.WriteLine(Resources.MuteIssue_NotPermitted, issueServerKey, response.notPermittedReason);
            throw new MuteIssueException(response.notPermittedReason);
        }

        return response.allowedStatuses;
    }

    private async Task MuteIssueAsync(
        string configurationScopeId,
        string issueServerKey,
        IFilterableIssue issue,
        SonarQubeIssueTransition transition)
    {
        try
        {
            var issueSlCoreService = GetIssueSlCoreService();
            await issueSlCoreService.ChangeStatusAsync(new ChangeIssueStatusParams
            (
                configurationScopeId,
                issueServerKey,
                transition.ToSlCoreResolutionStatus(),
                false // Muting taints are not supported yet
            ));
            await UpdateRoslynSuppressionsAsync(issue, issueServerKey);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.MuteIssue_AnErrorOccurred, issueServerKey, ex.Message);
            throw new MuteIssueException(ex);
        }
    }

    private async Task AddCommentAsync(string configurationScopeId, string issueServerKey, string comment)
    {
        try
        {
            var issueSlCoreService = GetIssueSlCoreService();
            if (comment?.Trim() is { Length: > 0 })
            {
                await issueSlCoreService.AddCommentAsync(new AddIssueCommentParams(configurationScopeId, issueServerKey, comment));
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.MuteIssue_AddCommentFailed, issueServerKey, ex.Message);
            throw new MuteIssueException.MuteIssueCommentFailedException();
        }
    }

    /// <summary>
    /// The suppressed issues for roslyn are not dealt by SlCore, but are stored on disk, so we need to update them manually
    /// </summary>
    private async Task UpdateRoslynSuppressionsAsync(IFilterableIssue issue, string serverIssueKey)
    {
        if (issue is IFilterableRoslynIssue)
        {
            await roslynSuppressionUpdater.UpdateSuppressedIssuesAsync(isResolved: true, [serverIssueKey], new CancellationToken());
        }
    }
}
