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
                if (await ResolveIssueWithDialogAsync(issueServerKey, isTaintIssue))
                {
                    logger.WriteLine(Resources.MuteIssue_HaveMuted);
                }
            }
            catch (Exception ex)
            {
                logger.WriteLine(ex.ToString());
                Show(Resources.MuteIssue_FailedError);
            }
        }).Forget();

    public void ReopenIssue(string issueServerKey, bool isTaintIssue) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            try
            {
                if (await ReopenIssueAsync(issueServerKey, isTaintIssue))
                {
                    logger.WriteLine(Resources.ReopenIssue_Success);
                }
            }
            catch (Exception ex)
            {
                logger.WriteLine(ex.ToString());
                Show(Resources.MuteIssue_FailedError);
            }
        }).Forget();

    private async Task<bool> ResolveIssueWithDialogAsync(string issueServerKey, bool isTaintIssue)
    {
        var currentConfigScope = activeConfigScopeTracker.Current;
        if (!CheckIsInConnectedMode(currentConfigScope) || !CheckIssueServerKeyNotNullOrEmpty(issueServerKey))
        {
            return false;
        }

        var allowedStatuses = await GetAllowedStatusesAsync(issueServerKey);
        if (allowedStatuses is { Count: 0 })
        {
            return false;
        }

        var promptMuteIssueResolutionAsync = await PromptMuteIssueResolutionAsync(allowedStatuses);
        if (promptMuteIssueResolutionAsync is null)
        {
            return false;
        }

        var (status, comment) = promptMuteIssueResolutionAsync!.Value;
        return await reviewIssuesService.ReviewIssueAsync(
            issueServerKey,
            status,
            comment,
            isTaintIssue);
    }

    private async Task<(ResolutionStatus status, string comment)?> PromptMuteIssueResolutionAsync(
        IEnumerable<ResolutionStatus> allowedStatuses)
    {
        ChangeStatusWindowResponse windowResponse = null;
        var viewModel = changeIssueStatusViewModelFactory.CreateForIssue(null, allowedStatuses);

        await threadHandling.RunOnUIThreadAsync(() =>
            windowResponse = changeStatusWindowService.Show(viewModel));

        if (!windowResponse.Result)
        {
            return null;
        }

        var selectedStatus = windowResponse.SelectedStatus.GetCurrentStatus<ResolutionStatus>();
        var normalizedComment = viewModel.GetNormalizedComment();

        return (selectedStatus, normalizedComment);
    }

    private bool CheckIssueServerKeyNotNullOrEmpty(string issueServerKey)
    {
        if (!string.IsNullOrWhiteSpace(issueServerKey))
        {
            return true;
        }

        logger.WriteLine(Resources.MuteIssue_IssueNotFound);
        Show(Resources.MuteIssue_IssueNotFound);
        return false;
    }

    private bool CheckIsInConnectedMode(Core.ConfigurationScope.ConfigurationScope currentConfigScope)
    {
        if (currentConfigScope is { Id: not null, ConnectionId: not null })
        {
            return true;
        }

        logger.WriteLine(Resources.MuteIssue_NotInConnectedMode);
        Show(Resources.MuteIssue_NotInConnectedMode);
        return false;
    }

    private async Task<List<ResolutionStatus>> GetAllowedStatusesAsync(string issueServerKey)
    {
        var permissionArgs = await reviewIssuesService.CheckReviewIssuePermittedAsync(issueServerKey);

        switch (permissionArgs)
        {
            case ReviewIssuePermittedArgs permitted:
                return permitted.AllowedStatuses.ToList();
            case ReviewIssueNotPermittedArgs notPermitted:
                logger.WriteLine(Resources.MuteIssue_NotPermitted, issueServerKey, notPermitted.Reason);
                Show(string.Format(Resources.MuteIssue_NotPermitted, issueServerKey, notPermitted.Reason));
                return [];
            default:
                throw new InvalidOperationException("Unknown permissions status");
        }
    }

    private async Task<bool> ReopenIssueAsync(string issueServerKey, bool isTaintIssue)
    {
        var currentConfigScope = activeConfigScopeTracker.Current;
        if (CheckIsInConnectedMode(currentConfigScope) && CheckIssueServerKeyNotNullOrEmpty(issueServerKey))
        {
            return await reviewIssuesService.ReopenIssueAsync(issueServerKey, isTaintIssue);
        }

        return false;
    }

    private void Show(string reason) => messageBox.Show(reason, Resources.MuteIssue_StatusChangeFailure, MessageBoxButton.OK, MessageBoxImage.Exclamation);
}
