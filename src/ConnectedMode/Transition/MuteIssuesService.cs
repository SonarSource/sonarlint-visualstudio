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
using SonarLint.VisualStudio.ConnectedMode.ReviewStatus;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Transition;

[Export(typeof(IMuteIssuesService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class MuteIssuesService(
    IChangeStatusWindowService changeStatusWindowService,
    IReviewIssuesService reviewIssuesService,
    IChangeIssueStatusViewModelFactory changeIssueStatusViewModelFactory,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IMessageBox messageBox,
    ILogger logger,
    IThreadHandling threadHandling)
    : IMuteIssuesService
{
    private readonly ILogger logger = logger.ForContext(nameof(MuteIssuesService));

    public void ResolveIssueWithDialog(string issueServerKey, bool isTaintIssue) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            try
            {
                await ResolveIssueWithDialogAsync(issueServerKey, isTaintIssue);
                logger.WriteLine(Resources.MuteIssue_HaveMuted);
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

    public void ReopenIssue(string issueServerKey, bool isTaintIssue) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            try
            {
                await ReopenIssueAsync(issueServerKey, isTaintIssue);
                logger.WriteLine(Resources.ReopenIssue_Success);
            }
            catch (ReopenIssueException ex)
            {
                messageBox.Show(ex.Message, Resources.ReopenIssue_FailureCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }).Forget();

    private async Task ResolveIssueWithDialogAsync(string issueServerKey, bool isTaintIssue)
    {
        var currentConfigScope = activeConfigScopeTracker.Current;
        CheckIsInConnectedMode(currentConfigScope);
        CheckIssueServerKeyNotNullOrEmpty(issueServerKey);

        var allowedStatuses = await GetAllowedStatusesAsync(issueServerKey);
        var (status, comment) = await PromptMuteIssueResolutionAsync(allowedStatuses);
        await ReviewIssueAsync(issueServerKey, status, comment, isTaintIssue);
    }

    private async Task<(ResolutionStatus status, string comment)> PromptMuteIssueResolutionAsync(
        IEnumerable<ResolutionStatus> allowedStatuses)
    {
        ChangeStatusWindowResponse windowResponse = null;
        var viewModel = changeIssueStatusViewModelFactory.CreateForIssue(null, allowedStatuses);

        await threadHandling.RunOnUIThreadAsync(() =>
            windowResponse = changeStatusWindowService.Show(viewModel));

        if (!windowResponse.Result)
        {
            throw new MuteIssueException.MuteIssueCancelledException();
        }

        var selectedStatus = windowResponse.SelectedStatus.GetCurrentStatus<ResolutionStatus>();
        var normalizedComment = viewModel.GetNormalizedComment();

        return (selectedStatus, normalizedComment);
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

    private async Task<List<ResolutionStatus>> GetAllowedStatusesAsync(string issueServerKey)
    {
        var permissionArgs = await reviewIssuesService.CheckReviewIssuePermittedAsync(issueServerKey);

        if (permissionArgs is ReviewIssueNotPermittedArgs notPermitted)
        {
            logger.WriteLine(Resources.MuteIssue_NotPermitted, issueServerKey, notPermitted.Reason);
            throw new MuteIssueException(notPermitted.Reason);
        }

        if (permissionArgs is ReviewIssuePermittedArgs permitted)
        {
            return permitted.AllowedStatuses.ToList();
        }

        // Should not happen
        throw new MuteIssueException("Unexpected permission check response");
    }

    private async Task ReviewIssueAsync(
        string issueServerKey,
        ResolutionStatus status,
        string comment,
        bool isTaintIssue)
    {
        var success = await reviewIssuesService.ReviewIssueAsync(
            issueServerKey,
            status,
            comment,
            isTaintIssue);

        if (!success)
        {
            logger.WriteLine(Resources.MuteIssue_AnErrorOccurred, issueServerKey, "See previous log entries");
            throw new MuteIssueException(Resources.MuteIssue_AnErrorOccurred);
        }
    }

    private async Task ReopenIssueAsync(string issueServerKey, bool isTaintIssue)
    {
        var currentConfigScope = activeConfigScopeTracker.Current;
        CheckIsInConnectedMode(currentConfigScope);
        CheckIssueServerKeyNotNullOrEmpty(issueServerKey);

        var success = await reviewIssuesService.ReopenIssueAsync(issueServerKey, isTaintIssue);

        if (!success)
        {
            logger.WriteLine(Resources.ReopenIssue_AnErrorOccurred, issueServerKey);
            throw new ReopenIssueException(Resources.ReopenIssue_AnErrorOccurred);
        }
    }
}

internal class ReopenIssueException(string message) : Exception(message);
