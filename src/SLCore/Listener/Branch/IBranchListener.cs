/*
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

using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Core;

namespace SonarLint.VisualStudio.SLCore.Listener.Branch;

public interface IBranchListener : ISLCoreListener
{
    /// <summary>
    /// Stub method for compability with SLCore.
    /// </summary>
    /// <param name="parameters">Parameter's here for compability we discard it</param>
    /// <remarks>This will be implemented properly in the future when needed but features we support does not need branch awareness for now</remarks>
    Task<MatchSonarProjectBranchResponse> MatchSonarProjectBranchAsync(MatchSonarProjectBranchParams parameters);

    /// <summary>
    /// Stub method for compability with SLCore.
    /// </summary>
    /// <param name="parameters">Parameter's here for compability we discard it</param>
    /// <remarks>This will be implemented properly in the future when needed but features we support does not need branch awareness for now</remarks>
    Task DidChangeMatchedSonarProjectBranchAsync(object parameters);
}
