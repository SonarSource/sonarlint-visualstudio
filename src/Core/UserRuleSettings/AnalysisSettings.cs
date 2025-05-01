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
    private readonly ImmutableArray<string> userDefinedFileExclusions = ImmutableArray<string>.Empty;

    public ImmutableDictionary<string, RuleConfig> Rules { get; }

    public ImmutableDictionary<string, string> AnalysisProperties { get; }

    public ImmutableArray<string> UserDefinedFileExclusions
    {
        get => userDefinedFileExclusions;
        private init
        {
            userDefinedFileExclusions = value
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToImmutableArray();
            NormalizedFileExclusions = userDefinedFileExclusions
                .Select(NormalizePath)
                .ToImmutableArray();
        }
    }

    public ImmutableArray<string> NormalizedFileExclusions { get; private init; }

    public AnalysisSettings(ImmutableDictionary<string, RuleConfig> rules = null, IEnumerable<string> fileExclusions = null, ImmutableDictionary<string, string> analysisProperties = null)
    {
        Rules = rules ?? ImmutableDictionary<string, RuleConfig>.Empty;
        UserDefinedFileExclusions = fileExclusions?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
        AnalysisProperties = analysisProperties ?? ImmutableDictionary<string, string>.Empty;
    }

    public AnalysisSettings(Dictionary<string, RuleConfig> rules, IEnumerable<string> fileExclusions, Dictionary<string, string> analysisProperties = null) : this(
        rules.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase), fileExclusions, analysisProperties?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase))
    {
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
