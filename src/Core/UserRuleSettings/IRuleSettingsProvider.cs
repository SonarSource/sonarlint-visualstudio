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

using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core.UserRuleSettings
{
    public interface IRuleSettingsProviderFactory
    {
        /// <summary>
        /// Create <see cref="IRuleSettingsProvider"/> for a specific language
        /// </summary>
        /// <remarks>
        /// In connected mode we need to know which file to load and we get it from <see cref="Language.FileSuffixAndExtension"/>.
        /// </remarks>
        IRuleSettingsProvider Get(Language language);
    }

    public interface IRuleSettingsProvider
    {
        /// <summary>
        /// Get rule settings specified in connected mode, or in the settings.json file
        /// </summary>
        RulesSettings Get();
    }

    public class RuleSettingsProvider : IRuleSettingsProvider
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly IRulesSettingsSerializer serializer;
        private readonly Language language;
        private readonly ILogger logger;

        public RuleSettingsProvider(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IUserSettingsProvider userSettingsProvider,
            IRulesSettingsSerializer serializer,
            Language language,
            ILogger logger)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.userSettingsProvider = userSettingsProvider;
            this.serializer = serializer;
            this.language = language;
            this.logger = logger;
        }

        public RulesSettings Get()
        {
            RulesSettings settings = null;

            // If in connected mode, look for rule settings in the .sonarlint/sonarqube folder.
            var binding = this.activeSolutionBoundTracker.CurrentConfiguration;

            if (binding != null && binding.Mode != SonarLintMode.Standalone)
            {
                settings = FindConnectedModeSettings(binding);
                if (settings == null)
                {
                    logger.WriteLine(CoreStrings.UnableToLoadConnectedModeSettings);
                }
                else
                {
                    logger.WriteLine(CoreStrings.UsingConnectedModeSettings);
                }
            }

            // If we are not in connected mode or couldn't find the connected mode settings then fall back on the standalone settings.
            settings = settings ?? userSettingsProvider.UserSettings.RulesSettings;

            return settings;
        }

        private RulesSettings FindConnectedModeSettings(BindingConfiguration2 binding)
        {
            var filePath = binding.BuildPathUnderConfigDirectory(language.FileSuffixAndExtension);
            var settings = serializer.SafeLoad(filePath);

            return settings;
        }
    }
}
