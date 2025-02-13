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
        if (currentConfigScope?.Id == null || currentConfigScope.SonarProjectId == null)
        {
            logger.LogVerbose(Resources.MuteWindowService_NotInConnectedMode);
            return;
        }

        if (!slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService issueSlCoreService))
        {
            logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
            return;
        }

        MuteIssuesWindowResponse windowResponse = null;
        await threadHandling.RunOnUIThreadAsync(() => windowResponse = muteIssuesWindowService.Show());
        if (!windowResponse.Result)
        {
            return;
        }

        try
        {
            await MuteIssueWithCommentAsync(issueSlCoreService, currentConfigScope.Id, issueServerKey, windowResponse);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.MuteIssue_AnErrorOccurred, issueServerKey, ex.Message);
        }
    }

    private static async Task MuteIssueWithCommentAsync(
        IIssueSLCoreService issueSlCoreService,
        string configurationScopeId,
        string issueServerKey,
        MuteIssuesWindowResponse windowResponse)
    {
        var newStatus = MapSonarQubeIssueTransitionToSlCoreResolutionStatus(windowResponse.IssueTransition);
        await issueSlCoreService.ChangeStatusAsync(new ChangeIssueStatusParams
        (
            configurationScopeId,
            issueServerKey,
            newStatus,
            false
        ));

        if (windowResponse.Comment is { Length: > 0 })
        {
            await issueSlCoreService.AddCommentAsync(new AddIssueCommentParams(configurationScopeId, issueServerKey, windowResponse.Comment));
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
