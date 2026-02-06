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
using SonarLint.VisualStudio.ConnectedMode.ReviewStatus;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Issues.ReviewIssue;

[Export(typeof(IReviewIssuesService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ReviewIssuesService(
    IActiveConfigScopeTracker activeConfigScopeTracker,
    ISLCoreServiceProvider slCoreServiceProvider,
    ILogger logger,
    IThreadHandling threadHandling)
    : IReviewIssuesService
{
    private readonly ILogger logger = logger.ForContext(nameof(ReviewIssuesService));

    public Task<bool> ReviewIssueAsync(string issueKey, ResolutionStatus newStatus, string comment, bool isTaint = false) =>
        threadHandling.RunOnBackgroundThread(async () => await TryChangeIssueStatusAsync(issueKey, newStatus, comment, isTaint));

    public Task<IReviewIssuePermissionArgs> CheckReviewIssuePermittedAsync(string issueKey) =>
        threadHandling.RunOnBackgroundThread(async () => await TryCheckStatusChangePermittedAsync(issueKey));

    public Task<bool> ReopenIssueAsync(string issueKey, bool isTaint = false) =>
        threadHandling.RunOnBackgroundThread(async () => await TryReopenIssueAsync(issueKey, isTaint));

    private async Task<bool> TryChangeIssueStatusAsync(string issueKey, ResolutionStatus newStatus, string comment, bool isTaint)
    {
        try
        {
            if (!slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService issueSlCoreService))
            {
                logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
                return false;
            }
            await issueSlCoreService.ChangeStatusAsync(new ChangeIssueStatusParams(
                activeConfigScopeTracker.Current?.Id,
                issueKey,
                newStatus,
                isTaint));

            if (!string.IsNullOrWhiteSpace(comment))
            {
                await issueSlCoreService.AddCommentAsync(new AddIssueCommentParams(
                    activeConfigScopeTracker.Current?.Id,
                    issueKey,
                    comment));
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(new MessageLevelContext { Context = [nameof(issueKey), issueKey] }, Resources.ReviewIssueService_AnErrorOccurred, ex.Message);
            return false;
        }
        return true;
    }

    private async Task<IReviewIssuePermissionArgs> TryCheckStatusChangePermittedAsync(string issueKey)
    {
        CheckStatusChangePermittedResponse response;
        var messageLevelContext = new MessageLevelContext { Context = [nameof(issueKey), issueKey] };
        try
        {
            if (!slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService issueSlCoreService))
            {
                logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
                return new ReviewIssueNotPermittedArgs(SLCoreStrings.ServiceProviderNotInitialized);
            }
            var checkStatusChangePermittedParams = new CheckStatusChangePermittedParams(activeConfigScopeTracker.Current?.ConnectionId, issueKey);
            response = await issueSlCoreService.CheckStatusChangePermittedAsync(checkStatusChangePermittedParams);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(messageLevelContext, Resources.ReviewIssueService_AnErrorOccurred, ex.Message);
            return new ReviewIssueNotPermittedArgs(ex.Message);
        }

        if (response.permitted)
        {
            return new ReviewIssuePermittedArgs(response.allowedStatuses);
        }

        logger.WriteLine(messageLevelContext, Resources.ReviewIssueService_NotPermitted, response.notPermittedReason);
        return new ReviewIssueNotPermittedArgs(response.notPermittedReason);
    }

    private async Task<bool> TryReopenIssueAsync(string issueKey, bool isTaint)
    {
        try
        {
            if (!slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService issueSlCoreService))
            {
                logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
                return false;
            }
            var response = await issueSlCoreService.ReopenIssueAsync(new ReopenIssueParams(
                activeConfigScopeTracker.Current?.Id,
                issueKey,
                isTaint));
            return response.success;
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(new MessageLevelContext { Context = [nameof(issueKey), issueKey] }, Resources.ReviewIssueService_AnErrorOccurred, ex.Message);
            return false;
        }
    }
}
