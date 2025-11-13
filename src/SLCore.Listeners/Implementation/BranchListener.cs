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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Branch;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation
{
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    public class BranchListener(IServerBranchProvider serverBranchProvider, IActiveConfigScopeTracker activeConfigScopeTracker, ILogger log) : IBranchListener
    {
        private readonly ILogger log = log.ForContext(SLCoreStrings.SLCoreAnalysisConfigurationLogContext, SLCoreStrings.ConnectedMode_LogContext).ForVerboseContext(nameof(BranchListener));

        /// <summary>
        /// Request to calculate the matching branch between the local project and the sonar server
        /// </summary>
        public Task<MatchSonarProjectBranchResponse> MatchSonarProjectBranchAsync(MatchSonarProjectBranchParams parameters)
        {
            if (activeConfigScopeTracker.Current.Id is var currentId && currentId != parameters.configurationScopeId)
            {
                log.WriteLine(SLCoreStrings.ConfigurationScopeMismatch, parameters.configurationScopeId, currentId);
                return Task.FromResult(new MatchSonarProjectBranchResponse(null));
            }

            var matchingBranchName = serverBranchProvider.GetServerBranchName(parameters.allSonarBranchesNames
                .Select(x => new RemoteBranch(x, x == parameters.mainSonarBranchName))
                .ToList());

            return Task.FromResult(new MatchSonarProjectBranchResponse(matchingBranchName));
        }

        /// <summary>
        /// Handles calculated branch notification from SLCore
        /// </summary>
        public Task DidChangeMatchedSonarProjectBranchAsync(DidChangeMatchedSonarProjectBranchParams parameters)
        {
            if (!activeConfigScopeTracker.TryUpdateMatchedBranchOnCurrentConfigScope(parameters.configScopeId, parameters.newMatchedBranchName))
            {
                log.WriteLine(SLCoreStrings.ConfigurationScopeMismatch, parameters.configScopeId, activeConfigScopeTracker.Current.Id);
            }

            return Task.CompletedTask;
        }
    }
}
