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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Core.UserRuleSettings;

/**
 * Example config file - same format as the VS Code settings.json file, with the addition of "parameters"
 * and "severity", both of which are optional.
 {
     ...
     "sonarlint.rules": {
         "typescript:S2685": {
             "level": "on"
         },
         "javascript:EqEqEq": {
             "level": "on"
         },
         "cpp:S967": {
             "level": "off"
         },
         "c:CommentedCode": {
             "level": "on",
             "Parameters": {
               "key1": "value1",
               "key2": "value2"
             },
             "severity": "Critical"
         },
     },
     "sonarlint.analysisExcludesStandalone": "file1.cs,file2.cs"
     ...
 }
 */
public class AnalysisSettings
{
    private const string AnyDirectoryWildcard = "**";
    private static readonly string AnyRootPrefix = AnyDirectoryWildcard + Path.AltDirectorySeparatorChar;
    private readonly ImmutableArray<string> userDefinedFileExclusions = ImmutableArray<string>.Empty;

    [JsonProperty("sonarlint.rules")]
    [JsonConverter(typeof(ImmutableDictionaryIgnoreCaseConverter<string, RuleConfig>))]
    public ImmutableDictionary<string, RuleConfig> Rules { get; init; }

    [JsonProperty("sonarlint.analysisExcludesStandalone")]
    [JsonConverter(typeof(CommaSeparatedStringArrayConverter))]
    public ImmutableArray<string> UserDefinedFileExclusions
    {
        get => userDefinedFileExclusions;
        init
        {
            userDefinedFileExclusions = value;
            NormalizedFileExclusions = value.Select(NormalizePath).ToArray();
        }
    }

    [JsonIgnore]
    public string[] NormalizedFileExclusions { get; private init; } = [];

    public AnalysisSettings(Dictionary<string, RuleConfig> rules, IEnumerable<string> fileExclusions)
    {
        Rules = rules.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        UserDefinedFileExclusions = fileExclusions.ToImmutableArray();
    }

    public AnalysisSettings(ImmutableDictionary<string, RuleConfig> rules, IEnumerable<string> fileExclusions) : this(rules.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), fileExclusions) { }

    public AnalysisSettings()
    {
        Rules = ImmutableDictionary.Create<string, RuleConfig>(StringComparer.OrdinalIgnoreCase);
        UserDefinedFileExclusions = ImmutableArray<string>.Empty;
    }

    private static string NormalizePath(string path)
    {
        // rooted paths without drive letter are unmodified, but SLCore doesn't match them well
        var isRooted = Path.IsPathRooted(path);

        path = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!isRooted && !path.StartsWith(AnyRootPrefix))
        {
            path = AnyRootPrefix + path;
        }

        return path;
    }
}

public class RuleConfig
{
    [JsonProperty("level")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RuleLevel Level { get; set; }

    // Note: property will be null if "parameters" is missing from the file.
    // This is what we want: most rules won't have parameters and we want to avoid
    // creating hundreds of unnecessary empty dictionaries.
    // The only downside is that the dictionary that is created will use the default
    // comparer, which is case-sensitive.
    [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string> Parameters { get; set; }
}

public enum RuleLevel
{
    On,
    Off
}
