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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.CFamily
{
    public class CFamilyRulesConfigurationProvider : IRulesConfigurationProvider
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;

        public CFamilyRulesConfigurationProvider(ISonarQubeService sonarQubeService, ILogger logger)
        {
            this.sonarQubeService = sonarQubeService ?? throw new ArgumentNullException(nameof(sonarQubeService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IRulesConfigurationProvider implementation

        public async Task<IRulesConfigurationFile> GetRulesConfigurationAsync(SonarQubeQualityProfile qualityProfile, string organizationKey,
            Language language, CancellationToken cancellationToken)
        {
            var result = await WebServiceHelper.SafeServiceCallAsync(
                    () => sonarQubeService.GetAllRulesAsync(qualityProfile.Key, cancellationToken), logger);

            if (result == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var config = CreateUserSettingsFromQPRules(result);
            var configFile = new CFamilyRulesConfigurationFile(config);

            return configFile;
        }

        #endregion implementation

        internal /* for testing */ static UserSettings CreateUserSettingsFromQPRules(IList<SonarQubeRule> rules)
        {
            var settings = new UserSettings()
            {
                Rules = rules.ToDictionary(ToRuleConfigKey, ToRuleConfig)
            };

            return settings;
        }

        private static string ToRuleConfigKey(SonarQubeRule sonarQubeRule)
            => $"{sonarQubeRule.RepositoryKey}:{sonarQubeRule.Key}";

        private static RuleConfig ToRuleConfig(SonarQubeRule sonarQubeRule)
        {
            var config = new RuleConfig()
            {
                Level = sonarQubeRule.IsActive ? RuleLevel.On : RuleLevel.Off,
                Parameters = sonarQubeRule?.Parameters.ToDictionary(p => p.Key, p => p.Value)
            };

            return config;
        }
    }
}
