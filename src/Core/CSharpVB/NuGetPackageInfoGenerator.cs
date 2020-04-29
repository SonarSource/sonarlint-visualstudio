/*
 * SonarLint for Visual Studio
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
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.CSharpVB
{
    public static class NuGetPackageInfoGenerator
    {
        public static IEnumerable<NuGetPackageInfo> GetNuGetPackageInfos(IEnumerable<SonarQubeRule> activeRules,
            IDictionary<string, string> sonarProperties)
        {
            var propertyPrefixes = GetPluginPropertyPrefixes(activeRules);
            var packages = new List<NuGetPackageInfo>();

            foreach (var partialRepoKey in propertyPrefixes)
            {
                if (!sonarProperties.TryGetValue($"{partialRepoKey}.analyzerId", out var analyzerId) ||
                    !sonarProperties.TryGetValue($"{partialRepoKey}.pluginVersion", out var pluginVersion))
                {
                    continue;
                }

                packages.Add(new NuGetPackageInfo(analyzerId, pluginVersion));
            }

            return packages;
        }

        private static IEnumerable<string> GetPluginPropertyPrefixes(IEnumerable<SonarQubeRule> rules) =>
            rules.Select(r => r.TryGetRoslynPluginPropertyPrefix())
                .Distinct()
                .Where(r => !string.IsNullOrEmpty(r))
                .ToArray();
    }
}
