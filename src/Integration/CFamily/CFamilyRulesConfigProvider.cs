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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.CFamily
{
    [Export(typeof(ICFamilyRulesConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CFamilyRuleConfigProvider : ICFamilyRulesConfigProvider
    {
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly ILogger logger;
        private readonly IConfigurationProvider configurationProvider;

        private readonly RulesMetadataCache rulesMetadataCache;

        [ImportingConstructor]
        public CFamilyRuleConfigProvider(IHost host, IUserSettingsProvider userSettingsProvider, ILogger logger)
        {
            this.userSettingsProvider = userSettingsProvider;
            this.logger = logger;

            configurationProvider = host.GetService<IConfigurationProvider>();
            rulesMetadataCache = new RulesMetadataCache(CFamilyShared.CFamilyFilesDirectory);
        }

        #region IRulesConfigurationProvider implementation

        public ICFamilyRulesConfig GetRulesConfiguration(string languageKey)
        {
            // TODO: check whether in connected mode, and if so use the appropriate settings

            var config = new DynamicCFamilyRulesConfig(rulesMetadataCache.GetSettings(languageKey), userSettingsProvider.UserSettings);
            return config;
        }

        #endregion IRulesConfigurationProvider implementation
    }
}
