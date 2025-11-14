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
using Newtonsoft.Json.Converters;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Core.UserRuleSettings;

[method: JsonConstructor]
public class RuleConfig(RuleLevel level, Dictionary<string, string> parameters = null)
{
    [JsonProperty("level")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RuleLevel Level { get; init; } = level;

    // Note: property will be null if "parameters" is missing from the file.
    // This is what we want: most rules won't have parameters and we want to avoid
    // creating hundreds of unnecessary empty dictionaries.
    // The only downside is that the dictionary that is created will use the default
    // comparer, which is case-sensitive.
    [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(ImmutableDictionaryIgnoreCaseConverter<string, string>))]
    public ImmutableDictionary<string, string> Parameters { get; init; } = parameters?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
}

public enum RuleLevel
{
    On,
    Off
}
