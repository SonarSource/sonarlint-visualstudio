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

using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Exclusions
{
    [Export(typeof(IAnalyzableFileIndicator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AnalyzableFileIndicator : IAnalyzableFileIndicator
    {
        private readonly IExclusionSettingsStorage exclusionSettingsStorage;
        private readonly IGlobPatternMatcher globPatternMatcher;
        private readonly ILogger logger;

        [ImportingConstructor]
        public AnalyzableFileIndicator(IExclusionSettingsStorage exclusionSettingsStorage, ILogger logger)
            : this(exclusionSettingsStorage, new GlobPatternMatcher(logger), logger)
        {
        }

        internal AnalyzableFileIndicator(IExclusionSettingsStorage exclusionSettingsStorage, 
            IGlobPatternMatcher globPatternMatcher,
            ILogger logger)
        {
            this.exclusionSettingsStorage = exclusionSettingsStorage;
            this.globPatternMatcher = globPatternMatcher;
            this.logger = logger;
        }

        public bool ShouldAnalyze(string filePath)
        {
            var serverExclusions = exclusionSettingsStorage.GetSettings();

            if (serverExclusions == null)
            {
                logger.LogVerbose("[AnalyzableFileIndicator] No server settings were found.");
                return true;
            }
            //Sonarqube uses unix-style directory seperators, so we should replace windows style seperators. 
            filePath = filePath.Replace("\\", "/");
            var shouldAnalyze = IsIncluded(serverExclusions, filePath) && !IsExcluded(serverExclusions, filePath);

            logger.LogVerbose($"[AnalyzableFileIndicator]" +
                            $"\n  {serverExclusions}" +
                            $"\n  file: '{filePath}'" +
                            $"\n  should analyze: {shouldAnalyze} ");

            return shouldAnalyze;
        }

        /// <summary>
        /// Returns true/false if the file is considered included according to the specified pattern.
        /// </summary>
        /// <remarks>
        /// If there is no defined pattern, it means everything is included.
        /// Hence, we check if the array is empty OR if it contains a matching pattern.
        /// </remarks>
        private bool IsIncluded(ServerExclusions serverExclusions, string filePath) =>
            serverExclusions.Inclusions == null ||
            serverExclusions.Inclusions.Length == 0 ||
            serverExclusions.Inclusions.Any(x => IsMatch(x, filePath));

        /// <summary>
        /// Returns true/false if the file is considered excluded according to the specified patterns.
        /// </summary>
        /// <remarks>
        /// The file is considered excluded only if there is a defined pattern AND the file matches the pattern.
        /// Project-level exclusions take precedence over global exclusions.
        /// </remarks>
        private bool IsExcluded(ServerExclusions serverExclusions, string filePath) =>
            IsExcluded(serverExclusions.Exclusions, filePath) ||
            IsExcluded(serverExclusions.GlobalExclusions, filePath);

        private bool IsExcluded(string[] exclusions, string filePath) =>
            exclusions != null &&
            exclusions.Any(x => IsMatch(x, filePath));

        private bool IsMatch(string pattern, string filePath)
        {
            return globPatternMatcher.IsMatch(pattern, filePath);
        }
    }
}
