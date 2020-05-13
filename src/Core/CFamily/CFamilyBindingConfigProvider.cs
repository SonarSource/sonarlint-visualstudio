/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
    public class CFamilyBindingConfigProvider : IBindingConfigProvider
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;
        private readonly ISolutionBindingFilePathGenerator solutionBindingFilePathGenerator;

        public CFamilyBindingConfigProvider(ISonarQubeService sonarQubeService, ILogger logger)
            : this(sonarQubeService, logger, new SolutionBindingFilePathGenerator())
        {
        }

        internal CFamilyBindingConfigProvider(ISonarQubeService sonarQubeService, ILogger logger, ISolutionBindingFilePathGenerator solutionBindingFilePathGenerator)
        {
            this.sonarQubeService = sonarQubeService ?? throw new ArgumentNullException(nameof(sonarQubeService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.solutionBindingFilePathGenerator = solutionBindingFilePathGenerator ?? throw new ArgumentNullException(nameof(solutionBindingFilePathGenerator));
        }

        #region IBindingConfigProvider implementation

        public bool IsLanguageSupported(Language language)
        {
            return Language.Cpp.Equals(language) || Language.C.Equals(language);
        }

        public async Task<IBindingConfig> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, Language language, BindingConfiguration bindingConfiguration, CancellationToken cancellationToken)
        {
            if (!IsLanguageSupported(language))
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            var result = await WebServiceHelper.SafeServiceCallAsync(
                    () => sonarQubeService.GetAllRulesAsync(qualityProfile.Key, cancellationToken), logger);

            if (result == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var settings = CreateRulesSettingsFromQPRules(result);
            var settingsFilePath = solutionBindingFilePathGenerator.Generate(
                bindingConfiguration.BindingConfigDirectory, 
                bindingConfiguration.Project.ProjectKey,
                language.FileSuffixAndExtension);

            var configFile = new CFamilyBindingConfig(settings, settingsFilePath);

            return configFile;
        }

        #endregion IBindingConfigProvider implementation

        internal /* for testing */ static RulesSettings CreateRulesSettingsFromQPRules(IList<SonarQubeRule> rules)
        {
            var settings = new RulesSettings()
            {
                Rules = rules.ToDictionary(ToRuleConfigKey, ToRuleConfig)
            };

            return settings;
        }

        private static string ToRuleConfigKey(SonarQubeRule sonarQubeRule)
            => $"{sonarQubeRule.RepositoryKey}:{sonarQubeRule.Key}";

        private static RuleConfig ToRuleConfig(SonarQubeRule sonarQubeRule)
        {
            // Most rules don't have parameters, so to avoid creating objects unnecessarily
            // we'll leave the parameters as null unless there really are values.
            Dictionary<string, string> parameters = null;
            if ((sonarQubeRule.Parameters?.Count ?? 0) != 0)
            {
                parameters = sonarQubeRule.Parameters.ToDictionary(p => p.Key, p => p.Value);
            }

            var config = new RuleConfig()
            {
                Level = sonarQubeRule.IsActive ? RuleLevel.On : RuleLevel.Off,
                Parameters = parameters,
                Severity = Convert(sonarQubeRule.Severity)
            };

            return config;
        }

        internal /* for testing */ static IssueSeverity? Convert(SonarQubeIssueSeverity sonarQubeIssueSeverity)
        {
            switch(sonarQubeIssueSeverity)
            {
                case SonarQubeIssueSeverity.Blocker:
                    return IssueSeverity.Blocker;
                case SonarQubeIssueSeverity.Critical:
                    return IssueSeverity.Critical;
                case SonarQubeIssueSeverity.Info:
                    return IssueSeverity.Info;
                case SonarQubeIssueSeverity.Major:
                    return IssueSeverity.Major;
                case SonarQubeIssueSeverity.Minor:
                    return IssueSeverity.Minor;
                default:
                    return null;
            }
        }
    }
}
