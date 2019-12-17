/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionRuleStore : ISolutionBindingConfigFileStore
    {
        private readonly Dictionary<Language, ConfigFileInformation> availableFiles = new Dictionary<Language, ConfigFileInformation>();

        #region ISolutionRuleStore

        ConfigFileInformation ISolutionBindingConfigFileStore.GetConfigFileInformation(Language language)
        {
            ConfigFileInformation ruleSet;
            this.availableFiles.TryGetValue(language, out ruleSet);

            return ruleSet;
        }

        void ISolutionBindingConfigFileStore.RegisterKnownConfigFiles(IDictionary<Language, IBindingConfigFile> languageToFileMap)
        {
            languageToFileMap.Should().NotBeNull("Not expecting nulls");

            foreach (var rule in languageToFileMap)
            {
                availableFiles.Add(rule.Key, new ConfigFileInformation(rule.Value));
            }
        }

        #endregion ISolutionRuleStore

        #region Test helpers

        public void RegisterConfigFilePath(Language language, string path, IBindingConfigFile bindingConfig = null)
        {
            if (!this.availableFiles.ContainsKey(language))
            {
                bindingConfig = bindingConfig ?? new DotNetBindingConfigFile(new RuleSet("SonarQube"));
                this.availableFiles[language] = new ConfigFileInformation(bindingConfig);
            }

            this.availableFiles[language].NewFilePath = path;
        }

        #endregion Test helpers
    }
}
