/*
 * SonarQube Client
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarQube.Client
{
    /// <summary>
    /// Extensions method for the basic <see cref="ISonarQubeService"/> interface
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Fetch all rules in the specified quality, both active and inactive
        /// </summary>
        public static async Task<IList<SonarQubeRule>> GetAllRulesAsync(this ISonarQubeService sonarQubeService, string qualityProfileKey, CancellationToken token)
        {
            var activeRules = await sonarQubeService.GetRulesAsync(true, qualityProfileKey, token);
            token.ThrowIfCancellationRequested();

            var inactiveRules = await sonarQubeService.GetRulesAsync(false, qualityProfileKey, token);

            var allRules = (activeRules ?? Enumerable.Empty<SonarQubeRule>())
                .Union(inactiveRules ?? Enumerable.Empty<SonarQubeRule>())
                .ToList();
            return allRules;
        }
    }
}
