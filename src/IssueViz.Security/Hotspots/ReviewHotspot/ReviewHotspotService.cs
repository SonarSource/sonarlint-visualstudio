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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Hotspot;
using CheckStatusChangePermittedParams = SonarLint.VisualStudio.SLCore.Service.Hotspot.CheckStatusChangePermittedParams;
using CheckStatusChangePermittedResponse = SonarLint.VisualStudio.SLCore.Service.Hotspot.CheckStatusChangePermittedResponse;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;

[Export(typeof(IReviewHotspotsService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ReviewHotspotsService(
    IActiveConfigScopeTracker activeConfigScopeTracker,
    ISLCoreServiceProvider slCoreServiceProvider,
    ILogger logger,
    IThreadHandling threadHandling)
    : IReviewHotspotsService
{
    private readonly ILogger logger = logger.ForContext(nameof(ReviewHotspotsService));

    public Task<bool> ReviewHotspotAsync(string hotspotKey, HotspotStatus newStatus) => threadHandling.RunOnBackgroundThread(async () => await TryChangeHotspotStatusAsync(hotspotKey, newStatus));

    public Task<IReviewHotspotPermissionArgs> CheckReviewHotspotPermittedAsync(string hotspotKey) =>
        threadHandling.RunOnBackgroundThread(async () => await TryCheckStatusChangePermittedAsync(hotspotKey));

    private async Task<bool> TryChangeHotspotStatusAsync(string hotspotKey, HotspotStatus newStatus)
    {
        try
        {
            if (!slCoreServiceProvider.TryGetTransientService(out IHotspotSlCoreService hotspotSlCoreService))
            {
                logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
                return false;
            }
            await hotspotSlCoreService.ChangeStatusAsync(new ChangeHotspotStatusParams(activeConfigScopeTracker.Current?.Id, hotspotKey, newStatus.ToSlCoreHotspotStatus()));
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(new MessageLevelContext { Context = [nameof(hotspotKey), hotspotKey] }, Resources.ReviewHotspotService_AnErrorOccurred, ex.Message);
            return false;
        }
        return true;
    }

    private async Task<IReviewHotspotPermissionArgs> TryCheckStatusChangePermittedAsync(string hotspotKey)
    {
        CheckStatusChangePermittedResponse response;
        var messageLevelContext = new MessageLevelContext { Context = [nameof(hotspotKey), hotspotKey] };
        try
        {
            if (!slCoreServiceProvider.TryGetTransientService(out IHotspotSlCoreService hotspotSlCoreService))
            {
                logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
                return new ReviewHotspotNotPermittedArgs(SLCoreStrings.ServiceProviderNotInitialized);
            }
            var checkStatusChangePermittedParams = new CheckStatusChangePermittedParams(activeConfigScopeTracker.Current?.ConnectionId, hotspotKey);
            response = await hotspotSlCoreService.CheckStatusChangePermittedAsync(checkStatusChangePermittedParams);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(messageLevelContext, Resources.ReviewHotspotService_AnErrorOccurred, ex.Message);
            return new ReviewHotspotNotPermittedArgs(ex.Message);
        }

        if (response.permitted)
        {
            return new ReviewHotspotPermittedArgs(response.allowedStatuses.Select(x => x.ToHotspotStatus()));
        }

        logger.WriteLine(messageLevelContext, Resources.ReviewHotspotService_NotPermitted, response.notPermittedReason);
        return new ReviewHotspotNotPermittedArgs(response.notPermittedReason);
    }
}
