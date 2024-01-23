/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Configuration;

namespace SonarLint.VisualStudio.TypeScript.Rules
{
    internal interface IRulesProviderFactory
    {
        /// <summary>
        /// Returns a rules provider containing rules for the specified language repository
        /// </summary>
        IRulesProvider Create(string repoKey, Language language);
    }

    [Export(typeof(IRulesProviderFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RulesProviderFactory : IRulesProviderFactory
    {
        // Note: the file contains rules for both JavaScript and TypeScript rules
        internal const string RuleDefinitionsFilePathContractName = "SonarLint.TypeScript.RuleDefinitionsFilePath";

        private readonly string ruleMetadataFilePath;
        private readonly IRuleSettingsProviderFactory ruleSettingsProviderFactory;
        private readonly IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration;

        [ImportingConstructor]
        public RulesProviderFactory([Import(RuleDefinitionsFilePathContractName)] string ruleMetadataFilePath,
            IRuleSettingsProviderFactory ruleSettingsProviderFactory,
            IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration)
        {
            this.ruleMetadataFilePath = ruleMetadataFilePath;
            this.ruleSettingsProviderFactory = ruleSettingsProviderFactory;
            this.connectedModeFeaturesConfiguration = connectedModeFeaturesConfiguration;
        }

        public IRulesProvider Create(string repoKey, Language language)
        {
            if (string.IsNullOrEmpty(repoKey))
            {
                throw new ArgumentNullException(nameof(repoKey));
            }

            Debug.Assert(!repoKey.EndsWith(":"), "Not expecting the repoKey to end with the colon separator character");

            var allRules = Load(ruleMetadataFilePath);
            var filteredRules = FilterByRepo(repoKey + ":", allRules);

            var ruleSettingsProvider = ruleSettingsProviderFactory.Get(language);

            return new RulesProvider(filteredRules, new ActiveRulesCalculator(filteredRules, ruleSettingsProvider, connectedModeFeaturesConfiguration));
        }

        private static List<RuleDefinition> Load(string filePath) =>
            JsonConvert.DeserializeObject<List<RuleDefinition>>(File.ReadAllText(filePath, Encoding.UTF8));

        private static IReadOnlyCollection<RuleDefinition> FilterByRepo(string repoPrefix, IEnumerable<RuleDefinition> rules) =>
            rules.Where(x => x.RuleKey.StartsWith(repoPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }
}
