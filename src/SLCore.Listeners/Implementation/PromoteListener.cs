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
using SonarLint.VisualStudio.ConnectedMode.Promote;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Promote;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class PromoteListener(IPromoteNotification promoteNotification, IShowMessageNotification reviewCampaignNotification) : IPromoteListener
{
    public void PromoteExtraEnabledLanguagesInConnectedMode(PromoteExtraEnabledLanguagesInConnectedModeParams parameters)
    {
        var languagesToPromote = parameters.languagesToPromote
            .Select(x => x.ConvertToCoreLanguage())
            .ToList();
        promoteNotification.PromoteConnectedMode(parameters.configurationScopeId, languagesToPromote);
    }

    public async Task<ShowMessageRequestResponse> ShowMessageRequestAsync(ShowMessageRequestParams parameters)
    {
        var key = await reviewCampaignNotification.ShowAsync(parameters.message, parameters.actions);
        return new ShowMessageRequestResponse(key, false);
    }
}
