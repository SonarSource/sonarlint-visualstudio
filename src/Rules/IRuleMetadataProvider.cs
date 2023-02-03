/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Rules
{
    /// <summary>
    /// Returns rule metadata
    /// </summary>
    /// <remarks>
    /// The metadata is extracted from the Java plugins using the plugin API
    /// </remarks>
    public interface IRuleMetadataProvider
    {
        /// <summary>
        /// Fetches rule info for the specified language rule
        /// </summary>
        /// <returns>The rule info, or null if the language/rule was not recognised</returns>
        IRuleInfo GetRuleInfo(Language language, string ruleKey);
    }

    [Export(typeof(IRuleMetadataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RuleMetadataProvider : IRuleMetadataProvider
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public RuleMetadataProvider(ILogger logger)
        {
            this.logger = logger;
        }

        public IRuleInfo GetRuleInfo(Language language, string ruleKey)
            => language?.ServerLanguage?.Key == null
                ? null : LoadRuleInfo(language, ruleKey);
        
        private IRuleInfo LoadRuleInfo(Language language, string ruleKey)
        {
            var resourcePath = CalcFullResourceName(language, ruleKey);
            try
            {
                using (var stream = GetType().Assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var data = reader.ReadToEnd();
                            return JsonConvert.DeserializeObject<RuleInfo>(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogVerbose(Resources.MetadataProvider_ErrorLoadingJson, resourcePath, ex);
            }
            return null;
        }

        // e.g. SonarLint.VisualStudio.Rules.Embedded.cpp.S101.json
        private static string CalcFullResourceName(Language language, string ruleKey)
            => $"SonarLint.VisualStudio.Rules.Embedded.{language.ServerLanguage.Key}.{ruleKey}.json";
    }
}
