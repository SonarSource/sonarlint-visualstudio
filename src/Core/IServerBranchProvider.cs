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

using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core
{
    public interface IServerBranchProvider
    {
        /// <summary>
        /// Returns the Sonar server branch to use when requesting data
        /// </summary>
        /// <returns>The Sonar server branch name,
        /// or the name of the Sonar server branch marked as "Main" if the branch cannot be determined,
        /// or null if we are not in connected mode.
        /// </returns>
        /// <remarks>
        /// Only applies in connected mode.
        /// </remarks>
        Task<string> GetServerBranchNameAsync(CancellationToken token);
    }

    /// <summary>
    /// Stateful version of <see cref="IServerBranchProvider"/>
    /// </summary>
    /// <remarks>The implementation is a singleton that recalculates the Sonar branch name
    /// automatically when necessary e.g. when a different solution is opened
    /// </remarks>
    public interface IStatefulServerBranchProvider : IServerBranchProvider { }

}
