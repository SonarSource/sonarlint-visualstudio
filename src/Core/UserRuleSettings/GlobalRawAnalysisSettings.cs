/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using Newtonsoft.Json;
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
public class GlobalRawAnalysisSettings
{
    [JsonProperty("sonarlint.rules")]
    [JsonConverter(typeof(ImmutableDictionaryIgnoreCaseConverter<string, RuleConfig>))]
    public ImmutableDictionary<string, RuleConfig> Rules { get; init; }

    [JsonProperty("sonarlint.analysisExcludesStandalone")]
    [JsonConverter(typeof(CommaSeparatedStringArrayConverter))]
    public ImmutableArray<string> UserDefinedFileExclusions { get; init; }

    public GlobalRawAnalysisSettings() : this(ImmutableDictionary<string, RuleConfig>.Empty, ImmutableArray<string>.Empty) { }

    public GlobalRawAnalysisSettings(Dictionary<string, RuleConfig> rules, IEnumerable<string> fileExclusions) : this(rules.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase),
        fileExclusions.ToImmutableArray())
    {
    }

    public GlobalRawAnalysisSettings(ImmutableDictionary<string, RuleConfig> rules, ImmutableArray<string> fileExclusions)
    {
        Rules = rules;
        UserDefinedFileExclusions = fileExclusions;
    }
}
