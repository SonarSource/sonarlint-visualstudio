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

using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.Service.Branch;

[JsonRpcClass("branch")]
public interface ISonarProjectBranchSlCoreService : ISLCoreService
{
    /// <summary>
    /// Must be called when any change on the VCS might lead to a different sonar project branch being resolved (could be a different HEAD, a branch checkout).
    /// </summary>
    void DidVcsRepositoryChange(DidVcsRepositoryChangeParams parameters);

    /// <summary>
    /// Returns the currently matched Sonar Project branch.
    /// Might return a null value in <see cref="GetMatchedSonarProjectBranchResponse" /> if no matching happened.
    /// </summary>
    Task<GetMatchedSonarProjectBranchResponse> GetMatchedSonarProjectBranchAsync(GetMatchedSonarProjectBranchParams parameters);
}
