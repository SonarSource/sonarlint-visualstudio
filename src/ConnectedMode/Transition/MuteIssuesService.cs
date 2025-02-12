/*
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Transition;
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
    ILogger logger,
    IThreadHandling threadHandling)
    : IMuteIssuesService
{

    public async Task ResolveIssueWithDialogAsync(string issueServerKey)
    {
        threadHandling.ThrowIfOnUIThread();

        var currentConfigScope = activeConfigScopeTracker.Current;
        CheckIsInConnectedMode(currentConfigScope);
        CheckIssueServerKeyNotNull(issueServerKey);

        await GetAllowedStatusesAsync(currentConfigScope.ConnectionId, issueServerKey);
        var windowResponse = await PromptMuteIssueResolutionAsync();
        await MuteIssueWithCommentAsync(currentConfigScope.Id, issueServerKey, windowResponse);
    }

    private async Task<MuteIssuesWindowResponse> PromptMuteIssueResolutionAsync()
    {
        MuteIssuesWindowResponse windowResponse = null;
        await threadHandling.RunOnUIThreadAsync(() => windowResponse = muteIssuesWindowService.Show());

        if (windowResponse.Result)
        {
            return windowResponse;
        }

        throw new MuteIssueException.CancelledException();
    }

    private void CheckIssueServerKeyNotNull(string issueServerKey)
    {
        if (issueServerKey != null)
        {
            return;
        }

        logger.WriteLine(Resources.MuteIssue_IssueNotFound);
        throw new MuteIssueException.ServerIssueNotFoundException();
    }

    private void CheckIsInConnectedMode(Core.ConfigurationScope.ConfigurationScope currentConfigScope)
    {
        if (currentConfigScope is { Id: not null, ConnectionId: not null })
        {
            return;
        }

        logger.WriteLine(Resources.MuteIssue_NotInConnectedMode);
        throw new MuteIssueException.NotInConnectedModeException();
    }

    private IIssueSLCoreService GetIssueSlCoreService()
    {
        if (slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService issueSlCoreService))
        {
            return issueSlCoreService;
        }

        logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
        throw new MuteIssueException.UnavailableServiceProviderException();
    }

    private async Task<List<ResolutionStatus>> GetAllowedStatusesAsync(string connectionId, string issueServerKey)
    {
        var issueSlCoreService = GetIssueSlCoreService();

        CheckStatusChangePermittedResponse response;
        try
        {
            var checkStatusChangePermittedParams = new CheckStatusChangePermittedParams(connectionId, issueServerKey);
            response = await issueSlCoreService.CheckStatusChangePermittedAsync(checkStatusChangePermittedParams);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.MuteIssue_AnErrorOccurred, issueServerKey, ex.Message);
            throw new MuteIssueException.SlCoreException(ex);
        }

        if (!response.permitted)
        {
            throw new MuteIssueException.NotPermittedException(response.notPermittedReason);
        }
        return response.allowedStatuses;
    }

    private async Task MuteIssueWithCommentAsync(string configurationScopeId, string issueServerKey, MuteIssuesWindowResponse windowResponse)
    {
        var issueSlCoreService = GetIssueSlCoreService();

        try
        {
            var newStatus = MapSonarQubeIssueTransitionToSlCoreResolutionStatus(windowResponse.IssueTransition);
            await issueSlCoreService.ChangeStatusAsync(new ChangeIssueStatusParams
            (
                configurationScopeId,
                issueServerKey,
                newStatus,
                false // Muting taints are not supported yet
            ));

            if (windowResponse.Comment is { Length: > 0 })
            {
                await issueSlCoreService.AddCommentAsync(new AddIssueCommentParams(configurationScopeId, issueServerKey, windowResponse.Comment));
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.MuteIssue_AnErrorOccurred, issueServerKey, ex.Message);
            throw new MuteIssueException.SlCoreException(ex);
        }
    }

    private static ResolutionStatus MapSonarQubeIssueTransitionToSlCoreResolutionStatus(SonarQubeIssueTransition sonarQubeIssueTransition) =>
        sonarQubeIssueTransition switch
        {
            SonarQubeIssueTransition.FalsePositive => ResolutionStatus.FALSE_POSITIVE,
            SonarQubeIssueTransition.WontFix => ResolutionStatus.WONT_FIX,
            SonarQubeIssueTransition.Accept => ResolutionStatus.ACCEPT,
            _ => throw new ArgumentOutOfRangeException(nameof(sonarQubeIssueTransition), sonarQubeIssueTransition, null)
        };
}
