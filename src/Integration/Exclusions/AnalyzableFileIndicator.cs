/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Exclusions
{
    [Export(typeof(IAnalyzableFileIndicator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AnalyzableFileIndicator : IAnalyzableFileIndicator
    {
        private readonly IExclusionSettingsFileStorage exclusionSettingsFileStorage;
        private readonly IGlobPatternMatcher globPatternMatcher;

        [ImportingConstructor]
        public AnalyzableFileIndicator(IExclusionSettingsFileStorage exclusionSettingsFileStorage)
            : this(exclusionSettingsFileStorage, new GlobPatternMatcher())
        {
        }

        internal AnalyzableFileIndicator(IExclusionSettingsFileStorage exclusionSettingsFileStorage, 
            IGlobPatternMatcher globPatternMatcher)
        {
            this.exclusionSettingsFileStorage = exclusionSettingsFileStorage;
            this.globPatternMatcher = globPatternMatcher;
        }

        public bool ShouldAnalyze(string filePath)
        {
            var serverExclusions = exclusionSettingsFileStorage.GetSettings();

            if (serverExclusions == null)
            {
                return true;
            }

            var shouldAnalyze = IsIncluded() && !IsExcluded();

            return shouldAnalyze;

            bool IsIncluded() =>
                serverExclusions.Inclusions == null ||
                serverExclusions.Inclusions.Length == 0 ||
                serverExclusions.Inclusions.Any(x => globPatternMatcher.IsMatch(x, filePath));

            bool IsExcluded() =>
                serverExclusions.Exclusions != null &&
                serverExclusions.Exclusions.Any(x => globPatternMatcher.IsMatch(x, filePath));
        }
    }
}
