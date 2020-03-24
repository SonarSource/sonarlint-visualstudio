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
using SonarLint.VisualStudio.Integration;

/* The calculator implements simple cache to reduce the number of times the effective settings
 * are recalculated (and so reduce the pressure on the garbage collector - we are creating
 * multiple objects per rule and we have hundreds of rules).
 * 
 * The cache stores one item per language - in effect it stores the last calculated config for 
 * that language and returns it if the source rules config and source user settings haven't
 * changed.
 * 
 * Cache hits/missed are written to the output window.
 * 
 * Limitation: the cache is based on object identity since the source objects don't currently
 * have any other mechanism that could be used.
 * 
 * This works ok for standalone mode: the default rules config is static so the root object
 * will always be same, and the IUserSettingsProvider only reloads the settings.json file
 * when it changes.
 * 
 * However, the caching doesn't work in connected mode since the connected mode settings are
 * reloaded automatically every time -> object identities are different -> cache miss.
 * 
 */

namespace SonarLint.VisualStudio.Core.CFamily
{
    /// <summary>
    /// Returns the effective rules configuration to use i.e. overrides the defaults with
    /// values in the user settings.
    /// </summary>
    /// <remarks>The calculator has an internal cache to reduce unnecessary re-calculations of
    /// the effective settings (and the associated object allocations).</remarks>
    public class EffectiveRulesConfigCalculator
    {
        private readonly ILogger logger;
        private readonly RulesConfigCache configCache;

        public EffectiveRulesConfigCalculator(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentOutOfRangeException(nameof(logger));

            configCache = new RulesConfigCache();
        }

        public ICFamilyRulesConfig GetEffectiveRulesConfig(string languageKey, ICFamilyRulesConfig defaultRulesConfig, RulesSettings customRulesConfiguration)
        {
            if (defaultRulesConfig == null)
            {
                throw new ArgumentNullException(nameof(defaultRulesConfig));
            }

            // Optimisation - if there are no custom rules settings then just return the default
            if ((customRulesConfiguration?.Rules?.Count ?? 0) == 0)
            {
                logger.WriteLine(CoreStrings.EffectiveRules_NoCustomRulesSettings);
                return defaultRulesConfig;
            }

            var effectiveConfig = configCache.FindConfig(languageKey, defaultRulesConfig, customRulesConfiguration);
            if (effectiveConfig != null)
            {
                logger.WriteLine(CoreStrings.EffectiveRules_CacheHit);
            }
            else
            {
                logger.WriteLine(CoreStrings.EffectiveRules_CacheMiss);
                effectiveConfig = new DynamicCFamilyRulesConfig(defaultRulesConfig, customRulesConfiguration);
                configCache.Add(languageKey, defaultRulesConfig, customRulesConfiguration, effectiveConfig);
            }

            return effectiveConfig;
        }

        /// <summary>
        /// Simple cache based on object identity
        /// </summary>
        /// <remarks>The cache holds at most one entry per language.</remarks>
        internal class RulesConfigCache
        {
            private struct CacheEntry
            {
                public CacheEntry(ICFamilyRulesConfig sourceConfig, RulesSettings sourceSettings, ICFamilyRulesConfig effectiveConfig)
                {
                    SourceConfig = sourceConfig;
                    SourceSettings = sourceSettings;
                    EffectiveConfig = effectiveConfig;
                }

                public ICFamilyRulesConfig SourceConfig { get; }
                public RulesSettings SourceSettings { get; }
                public ICFamilyRulesConfig EffectiveConfig { get; }
            }

            private readonly IDictionary<string, CacheEntry> languageToConfigMap = new Dictionary<string, CacheEntry>();

            internal /* for testing */ int CacheCount { get { return languageToConfigMap.Count; } }

            public ICFamilyRulesConfig FindConfig(string languageKey, ICFamilyRulesConfig sourceConfig, RulesSettings sourceSettings)
            {
                if (!languageToConfigMap.TryGetValue(languageKey, out var cachedValue))
                {
                    return null;
                }

                if (object.ReferenceEquals(sourceConfig, cachedValue.SourceConfig) &&
                    object.ReferenceEquals(sourceSettings, cachedValue.SourceSettings))
                {
                    return cachedValue.EffectiveConfig;
                }

                languageToConfigMap.Remove(languageKey); // entry doesn't match -> remove it
                return null;
            }

            public void Add(string languageKey, ICFamilyRulesConfig sourceConfig, RulesSettings sourceSettings, ICFamilyRulesConfig effectiveConfig)
            {
                languageToConfigMap[languageKey] = new CacheEntry(sourceConfig, sourceSettings, effectiveConfig);
            }
        }
    }
}
