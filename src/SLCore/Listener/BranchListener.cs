/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Core;

namespace SonarLint.VisualStudio.SLCore.Listener
{
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class BranchListener : ISLCoreListener
    {
        /// <summary>
        /// Stub method for compability with SLCore.
        /// </summary>
        /// <param name="parameters">Parameter's here for compability we discard it</param>
        /// <remarks>This will be implemented properly in the future when needed but features we support does not need branch awareness for now</remarks>
        public async Task<MatchSonarProjectBranchResponse> MatchSonarProjectBranchAsync(MatchSonarProjectBranchParams parameters)
        {
            return new MatchSonarProjectBranchResponse { matchedSonarBranch = parameters.mainSonarBranchName };
        }

        /// <summary>
        /// Stub method for compability with SLCore.
        /// </summary>
        /// <param name="parameters">Parameter's here for compability we discard it</param>
        /// <remarks>This will be implemented properly in the future when needed but features we support does not need branch awareness for now</remarks>
        public Task DidChangeMatchedSonarProjectBranchAsync(object parameters)
        {
            return Task.CompletedTask;
        }
    }

    public class MatchSonarProjectBranchParams
    {
        public string configurationScopeId;
        public string mainSonarBranchName;
        public List<string> allSonarBranchesNames;
    }

    public class MatchSonarProjectBranchResponse
    {
        public string matchedSonarBranch;
    }
}
