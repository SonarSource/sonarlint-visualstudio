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

using System.IO;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

internal class SonarLintXmlProvider(ISonarLintConfigurationXmlSerializer sonarLintConfigurationXmlSerializer) : ISonarLintXmlProvider
{
    public SonarLintXmlConfiguration Create(IEnumerable<ActiveRoslynRule> activeRules, Dictionary<string, string>? analysisProperties)
    {
        var sonarLintConfiguration = new SonarLintConfiguration
        {
            Settings = ConvertDictionary(analysisProperties),
            Rules = activeRules
                .Select(x => new SonarLintRule { Key = x.RuleId.RuleKey, Parameters = ConvertDictionary(x.Parameters) })
                .ToList()
        };

        return new SonarLintXmlConfiguration(Path.GetTempPath(), sonarLintConfigurationXmlSerializer.Serialize(sonarLintConfiguration));
    }

    private static List<SonarLintKeyValuePair>? ConvertDictionary(Dictionary<string, string>? dictionary) => dictionary?.Select(ConvertKeyValuePair).ToList();

    private static SonarLintKeyValuePair ConvertKeyValuePair(KeyValuePair<string, string> x) => new() { Key = x.Key, Value = x.Value };
}
