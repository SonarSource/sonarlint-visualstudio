﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Branch;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation
{
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    public class BranchListener(IStatefulServerBranchProvider statefulServerBranchProvider) : IBranchListener
    {
        /// <summary>
        /// Request to calculate the matching branch between the local project and the sonar server
        /// </summary>
        public async Task<MatchSonarProjectBranchResponse> MatchSonarProjectBranchAsync(MatchSonarProjectBranchParams parameters)
        {
            var matchingBranchName = await statefulServerBranchProvider.GetServerBranchNameAsync(CancellationToken.None);
            return new MatchSonarProjectBranchResponse(matchingBranchName);
        }

        /// <summary>
        /// Stub method for compability with SLCore. TODO https://github.com/SonarSource/sonarlint-visualstudio/issues/5401
        /// </summary>
        /// <param name="parameters">Parameter's here for compability we discard it</param>
        /// <remarks>This will be implemented properly in the future when needed but features we support does not need branch awareness for now</remarks>
        public Task DidChangeMatchedSonarProjectBranchAsync(object parameters)
        {
            return Task.CompletedTask;
        }

        public Task<MatchProjectBranchResponse> MatchProjectBranchAsync(MatchProjectBranchParams parameters)
        {
            // At the moment we don't need to match the project branch as there is logic to handle the cases
            // where there is a mismatch between the project branch and the server branch
            // This is currently not fully supported because it depends on the showMessage method
            // https://sonarsource.atlassian.net/browse/SLVS-1494
            return Task.FromResult(new MatchProjectBranchResponse(true));
        }
    }
}
