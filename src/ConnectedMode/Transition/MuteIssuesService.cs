/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using System.Windows;
using Microsoft.VisualStudio.Threading;
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
    IMessageBox messageBox,
    ILogger logger,
    IThreadHandling threadHandling)
    : IMuteIssuesService
{
    private readonly ILogger logger = logger.ForContext(nameof(MuteIssuesService));

    public void ResolveIssueWithDialog(IFilterableIssue issue) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            try
            {
                await ResolveIssueWithDialogAsync(issue);
                logger.WriteLine(Resources.MuteIssue_HaveMuted);
            }
            catch (MuteIssueException.MuteIssueCommentFailedException)
            {
                messageBox.Show(Resources.MuteIssue_MessageBox_AddCommentFailed, Resources.MuteIssue_WarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (MuteIssueException.MuteIssueCancelledException)
            {
                // do nothing
            }
            catch (MuteIssueException ex)
            {
                messageBox.Show(ex.Message, Resources.MuteIssue_FailureCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }).Forget();

    private async Task ResolveIssueWithDialogAsync(IFilterableIssue issue)
    {
        var issueServerKey = GetIssueServerKey(issue);
        var currentConfigScope = activeConfigScopeTracker.Current;
        CheckIsInConnectedMode(currentConfigScope);
        CheckIssueServerKeyNotNullOrEmpty(issueServerKey);

        var allowedStatuses = await GetAllowedStatusesAsync(currentConfigScope.ConnectionId, issueServerKey);
        var windowResponse = await PromptMuteIssueResolutionAsync(allowedStatuses);
        await MuteIssueAsync(currentConfigScope.Id, issueServerKey, windowResponse.IssueTransition.Value);
        await AddCommentAsync(currentConfigScope.Id, issueServerKey, windowResponse.Comment);
    }

    private static string GetIssueServerKey(IFilterableIssue issue) =>
        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2419 remove handling of different type of issues
        ((IAnalysisIssueVisualization)issue).Issue.IssueServerKey;

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
}
