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

using System.Collections.Immutable;
using System.IO;

namespace SonarLint.VisualStudio.Core.UserRuleSettings;

public class AnalysisSettings
{
    private const string AnyDirectoryWildcard = "**";
    private static readonly string AnyRootPrefix = AnyDirectoryWildcard + Path.AltDirectorySeparatorChar;
    private readonly ImmutableArray<string> globalFileExclusions = ImmutableArray<string>.Empty;
    private readonly ImmutableArray<string> solutionFileExclusions = ImmutableArray<string>.Empty;
    private readonly ImmutableArray<string> normalizedFileExclusions = ImmutableArray<string>.Empty;

    public ImmutableDictionary<string, RuleConfig> Rules { get; }

    public ImmutableDictionary<string, string> AnalysisProperties { get; }

    public ImmutableArray<string> GlobalFileExclusions
    {
        get => globalFileExclusions;
        private init =>
            globalFileExclusions = value
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToImmutableArray();
    }

    public ImmutableArray<string> SolutionFileExclusions
    {
        get => solutionFileExclusions;
        private init =>
            solutionFileExclusions = value
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToImmutableArray();
    }

    public ImmutableArray<string> NormalizedFileExclusions
    {
        get => normalizedFileExclusions;
        private init =>
            normalizedFileExclusions = value.Select(NormalizePath)
                .ToImmutableArray();
    }

    public AnalysisSettings(
        Dictionary<string, RuleConfig> rules,
        IEnumerable<string> globalFileExclusions,
        IEnumerable<string> solutionFileExclusions = null,
        Dictionary<string, string> analysisProperties = null) : this(
        rules.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase), globalFileExclusions?.ToImmutableArray(), solutionFileExclusions?.ToImmutableArray(),
        analysisProperties?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase))
    {
    }

    public AnalysisSettings(
        ImmutableDictionary<string, RuleConfig> rules = null,
        ImmutableArray<string>? globalFileExclusions = null,
        ImmutableArray<string>? solutionFileExclusions = null,
        ImmutableDictionary<string, string> analysisProperties = null)
    {
        Rules = rules ?? ImmutableDictionary<string, RuleConfig>.Empty;
        GlobalFileExclusions = globalFileExclusions ?? ImmutableArray<string>.Empty;
        SolutionFileExclusions = solutionFileExclusions ?? ImmutableArray<string>.Empty;
        NormalizedFileExclusions = SolutionFileExclusions.IsEmpty ? GlobalFileExclusions : SolutionFileExclusions;
        AnalysisProperties = analysisProperties ?? ImmutableDictionary<string, string>.Empty;
    }

    private static string NormalizePath(string path)
    {
        path = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        // if path is invalid, the Path.IsPathRooted check will throw an exception
        if (HasInvalidPathChars(path))
        {
            return path;
        }

        // rooted paths without drive letter are unmodified, but SLCore doesn't match them well
        var isRooted = Path.IsPathRooted(path);
        if (!isRooted && !path.StartsWith(AnyRootPrefix))
        {
            path = AnyRootPrefix + path;
        }

        return path;
    }

    private static bool HasInvalidPathChars(string path)
    {
        var invalidChars = Path.GetInvalidPathChars();
        return path.IndexOfAny(invalidChars) >= 0;
    }
}
