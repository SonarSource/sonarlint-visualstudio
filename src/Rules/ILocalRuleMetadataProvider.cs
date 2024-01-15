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
using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Rules
{
    /// <summary>
    /// Returns rule metadata
    /// </summary>
    /// <remarks>
    /// The metadata is extracted from the Java plugins using the plugin API
    /// </remarks>
    public interface ILocalRuleMetadataProvider
    {
        /// <summary>
        /// Fetches rule info for the specified language rule
        /// </summary>
        /// <returns>The rule info, or null if the repo key/rule was not recognised</returns>
        IRuleInfo GetRuleInfo(SonarCompositeRuleId ruleId);
    }

    [Export(typeof(ILocalRuleMetadataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LocalRuleMetadataProvider : ILocalRuleMetadataProvider
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public LocalRuleMetadataProvider(ILogger logger)
        {
            this.logger = logger;
        }

        public IRuleInfo GetRuleInfo(SonarCompositeRuleId ruleId)
                => LoadRuleInfo(ruleId);

        private IRuleInfo LoadRuleInfo(SonarCompositeRuleId ruleId)
        {
            var resourcePath = CalcFullResourceName(ruleId);
            try
            {
                using (var stream = GetType().Assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var data = reader.ReadToEnd();
                            return RuleInfoJsonDeserializer.Deserialize(data);
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
        // e.g. SonarLint.VisualStudio.Rules.Embedded.typescript.S202.json
        // e.g. SonarLint.VisualStudio.Rules.Embedded.csharpsquid.S303.json
        private static string CalcFullResourceName(SonarCompositeRuleId ruleId)
            => $"SonarLint.VisualStudio.Rules.Embedded.{ruleId.RepoKey}.{ruleId.RuleKey}.json";


        /// <summary>
        /// Custom deserializer for rule info JSON
        /// </summary>
        /// <remark>
        /// RuleInfo has properties that are typed as interfaces, so NewtonSoft.Json needs more info
        /// about which concrete types to use when deserializing
        /// </remark>
        public static class RuleInfoJsonDeserializer
        {
            private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Converters = {
                    new InterfaceToConcreteTypeConverter<IRuleInfo, RuleInfo>(),
                    new InterfaceToConcreteTypeConverter<IDescriptionSection, DescriptionSection>(),
                    new InterfaceToConcreteTypeConverter<IContext, Context>()
                }
            };

            public static IRuleInfo Deserialize(string json)
                => JsonConvert.DeserializeObject<RuleInfo>(json, settings);
        }
    }
}
