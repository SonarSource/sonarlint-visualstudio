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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.CFamily.Rules
{
    [Export(typeof(ICFamilyRulesConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CFamilyRuleConfigProvider : ICFamilyRulesConfigProvider
    {
        private readonly EffectiveRulesConfigCalculator effectiveConfigCalculator;

        // Settable in constructor for testing
        private readonly IRuleSettingsProviderFactory ruleSettingsProviderFactory;
        private readonly ICFamilyRulesConfigProvider sonarWayProvider;


        [ImportingConstructor]
        public CFamilyRuleConfigProvider(IRuleSettingsProviderFactory ruleSettingsProviderFactory,
            IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration,
            ILogger logger)
            : this(ruleSettingsProviderFactory,
                 new CFamilySonarWayRulesConfigProvider(CFamilyShared.CFamilyFilesDirectory),
                 connectedModeFeaturesConfiguration,
                 logger)
        {
        }

        public CFamilyRuleConfigProvider(IRuleSettingsProviderFactory ruleSettingsProviderFactory,
            ICFamilyRulesConfigProvider sonarWayProvider,
            IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration,
            ILogger logger)
        {
            this.ruleSettingsProviderFactory = ruleSettingsProviderFactory;
            this.sonarWayProvider = sonarWayProvider;
            this.effectiveConfigCalculator = new EffectiveRulesConfigCalculator(connectedModeFeaturesConfiguration, logger);
        }

        #region IRulesConfigurationProvider implementation

        public ICFamilyRulesConfig GetRulesConfiguration(string languageKey)
        {
            if (languageKey == null)
            {
                throw new ArgumentNullException(nameof(languageKey));
            }

            var language = Language.GetLanguageFromLanguageKey(languageKey);

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            var ruleSettingsProvider = ruleSettingsProviderFactory.Get(language);
            var settings = ruleSettingsProvider.Get();

            var sonarWayConfig = sonarWayProvider.GetRulesConfiguration(languageKey);
            return CreateConfiguration(languageKey, sonarWayConfig, settings);
        }

        #endregion IRulesConfigurationProvider implementation

        protected virtual /* for testing */ ICFamilyRulesConfig CreateConfiguration(string languageKey, ICFamilyRulesConfig sonarWayConfig, RulesSettings settings)
            => effectiveConfigCalculator.GetEffectiveRulesConfig(languageKey, sonarWayConfig, settings);
    }
}
