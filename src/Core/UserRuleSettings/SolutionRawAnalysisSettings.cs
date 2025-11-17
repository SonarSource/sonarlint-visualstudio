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

public class SolutionRawAnalysisSettings
{
    [JsonProperty("sonarlint.analyzerProperties")]
    [JsonConverter(typeof(ImmutableDictionaryIgnoreCaseConverter<string, string>))]
    public ImmutableDictionary<string, string> AnalysisProperties { get; init; }

    [JsonProperty("sonarlint.analysisExcludesStandalone")]
    [JsonConverter(typeof(CommaSeparatedStringArrayConverter))]
    public ImmutableArray<string> UserDefinedFileExclusions { get; init; }

    public SolutionRawAnalysisSettings() : this(ImmutableDictionary<string, string>.Empty, ImmutableArray<string>.Empty) { }

    public SolutionRawAnalysisSettings(Dictionary<string, string> analysisProperties, IEnumerable<string> fileExclusions) : this(
        analysisProperties.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase), fileExclusions.ToImmutableArray())
    {
    }

    public SolutionRawAnalysisSettings(ImmutableDictionary<string, string> analysisProperties, ImmutableArray<string> fileExclusions)
    {
        AnalysisProperties = analysisProperties;
        UserDefinedFileExclusions = fileExclusions;
    }
}
