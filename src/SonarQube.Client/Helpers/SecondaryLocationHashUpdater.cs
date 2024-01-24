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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarQube.Client.Helpers
{
    /// <summary>
    /// Sets the hashes for any secondary locations in the supplied list of issues
    /// </summary>
    /// <remarks>
    /// Currently secondary location hashes are not stored server-side, so we have to
    /// calculate them ourselves. This means fetching the source code for each file
    /// so we can get the line text and calculate the hash.
    /// </remarks>
    internal interface ISecondaryIssueHashUpdater
    {
        Task UpdateHashesAsync(IEnumerable<SonarQubeIssue> issues,
            ISonarQubeService sonarQubeService,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// This component does not have any mutable state, so it can safely handle multiple
    /// concurrent calls.
    /// </summary>
    internal class SecondaryLocationHashUpdater : ISecondaryIssueHashUpdater
    {
        private readonly IChecksumCalculator checksumCalculator;

        public SecondaryLocationHashUpdater()
            : this(new ChecksumCalculator())
        {
        }

        internal /* for testing */ SecondaryLocationHashUpdater(IChecksumCalculator checksumCalculator)
        {
            this.checksumCalculator = checksumCalculator;
        }

        public async Task UpdateHashesAsync(IEnumerable<SonarQubeIssue> issues,
            ISonarQubeService sonarQubeService,
            CancellationToken cancellationToken)
        {
            var secondaryLocations = GetSecondaryLocations(issues);
            if (!secondaryLocations.Any())
            {
                // This will be the normal case: most issues don't have secondary locations
                return;
            }

            var uniqueKeys = GetUniqueSecondaryLocationKeys(secondaryLocations);

            var map = new ModuleKeyToSourceMap();
            foreach (var key in uniqueKeys)
            {
                var sourceCode = await sonarQubeService.GetSourceCodeAsync(key, cancellationToken);
                Debug.Assert(sourceCode != null, "Not expecting the file contents to be null");
                map.AddSourceCode(key, sourceCode);
            }

            foreach (var location in GetSecondaryLocations(issues))
            {
                SetLineHash(map, location);
            }
        }

        private static IEnumerable<IssueLocation> GetSecondaryLocations(IEnumerable<SonarQubeIssue> issues) =>
            issues.SelectMany(
                issue => issue.Flows.SelectMany(
                    flow => flow.Locations))
            .ToArray();

        private static IEnumerable<string> GetUniqueSecondaryLocationKeys(IEnumerable<IssueLocation> locations) =>
            locations
                .Select(loc => loc.ModuleKey)
                .Distinct()
                .ToArray();

        private void SetLineHash(ModuleKeyToSourceMap map, IssueLocation location)
        {
            // Issue locations can span multiple lines, but only the first line is used
            // when calculating the hash
            var firstLineOfIssue = map.GetLineText(location.ModuleKey, location.TextRange.StartLine);

            if (firstLineOfIssue != null)
            {
                location.Hash = checksumCalculator.Calculate(firstLineOfIssue);
            }
        }

        /// <summary>
        /// Provide a lookup from module key -> source code line
        /// </summary>
        private sealed class ModuleKeyToSourceMap
        {
            private readonly IDictionary<string, string[]> keyToLinesMap = new Dictionary<string, string[]>();

            public void AddSourceCode(string moduleKey, string sourceCode)
            {
                keyToLinesMap.Add(moduleKey, sourceCode.Split('\n'));
            }

            public string GetLineText(string moduleKey, int oneBasedLineNumber)
            {
                Debug.Assert(keyToLinesMap.ContainsKey(moduleKey), "Unexpected module key requested");

                var lines = keyToLinesMap[moduleKey];

                if (oneBasedLineNumber > lines.Length)
                {
                    return null;
                }

                return lines[oneBasedLineNumber - 1];
            }
        }
    }
}
