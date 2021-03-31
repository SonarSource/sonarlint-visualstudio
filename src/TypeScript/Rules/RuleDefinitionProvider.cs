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
using System.Text;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.TypeScript.Rules
{
    // TODO: same or different file for JS rules?

    internal interface IRuleDefinitionProvider
    {
        /// <summary>
        /// Returns the metadata descriptions for all TypeScript rules
        /// </summary>
        IEnumerable<RuleDefinition> GetAllRules();
    }

    [Export(typeof(IRuleDefinitionProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RuleDefinitionProvider : IRuleDefinitionProvider
    {
        internal const string TypeScriptMetadataFilePathContractName = "SonarLint.TypeScript.TypeScriptRulesMetadataFilePath";

        private readonly IReadOnlyCollection<RuleDefinition> rules;

        [ImportingConstructor]
        public RuleDefinitionProvider([Import(TypeScriptMetadataFilePathContractName)] string typeScriptMetadataFilePath)
            => rules = Load(typeScriptMetadataFilePath).AsReadOnly();

        public IEnumerable<RuleDefinition> GetAllRules() => rules;

        internal static List<RuleDefinition> Load(string filePath) =>
            JsonConvert.DeserializeObject<List<RuleDefinition>>(File.ReadAllText(filePath, Encoding.UTF8));
    }
}
