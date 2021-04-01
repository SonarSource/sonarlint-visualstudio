/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.TypeScript.Rules
{
    internal interface ITypeScriptRuleDefinitionsProvider
    {
        /// <summary>
        /// Returns the metadata descriptions for all TypeScript rules
        /// </summary>
        IEnumerable<RuleDefinition> GetDefinitions();
    }

    internal interface IJavaScriptRuleDefinitionsProvider
    {
        /// <summary>
        /// Returns the metadata descriptions for all JavaScript rules
        /// </summary>
        IEnumerable<RuleDefinition> GetDefinitions();
    }

    [Export(typeof(ITypeScriptRuleDefinitionsProvider))]
    [Export(typeof(IJavaScriptRuleDefinitionsProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RuleDefinitionsProvider : ITypeScriptRuleDefinitionsProvider, IJavaScriptRuleDefinitionsProvider
    {
        // Note: the file contains rules for both JavaScript and TypeScript rules
        internal const string RuleDefinitionsFilePathContractName = "SonarLint.TypeScript.RuleDefinitionsFilePath";

        private readonly IReadOnlyCollection<RuleDefinition> tsRules;
        private readonly IReadOnlyCollection<RuleDefinition> jsRules;

        private const string RepoPrefix_TypeScript = "typescript:";
        private const string RepoPrefix_JavaScript = "javascript:";

        [ImportingConstructor]
        public RuleDefinitionsProvider([Import(RuleDefinitionsFilePathContractName)] string typeScriptMetadataFilePath)
        {
            var allRules = Load(typeScriptMetadataFilePath);
            tsRules = FilterByRepo(RepoPrefix_TypeScript, allRules);
            jsRules = FilterByRepo(RepoPrefix_JavaScript, allRules);
        }

        IEnumerable<RuleDefinition> ITypeScriptRuleDefinitionsProvider.GetDefinitions() => tsRules;

        IEnumerable<RuleDefinition> IJavaScriptRuleDefinitionsProvider.GetDefinitions() => jsRules;

        private static List<RuleDefinition> Load(string filePath) =>
            JsonConvert.DeserializeObject<List<RuleDefinition>>(File.ReadAllText(filePath, Encoding.UTF8));

        private static IReadOnlyCollection<RuleDefinition> FilterByRepo(string repoPrefix, IEnumerable<RuleDefinition> rules) =>
            rules.Where(x => x.RuleKey.StartsWith(repoPrefix, System.StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }
}
