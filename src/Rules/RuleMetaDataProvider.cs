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

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Configuration;

namespace SonarLint.VisualStudio.Rules
{
    public interface IRuleMetaDataProvider
    {
        /// <summary>
        /// Returns rule information for the specified rule ID, or null if a rule description
        /// could not be found.
        /// </summary>
        Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, CancellationToken token);
    }

    [Export(typeof(IRuleMetaDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RuleMetaDataProvider : IRuleMetaDataProvider
    {
        private readonly ILocalRuleMetadataProvider localRuleMetadataProvider;
        private readonly IServerRuleMetadataProvider serverRuleMetadataProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration;

        [ImportingConstructor]
        public RuleMetaDataProvider(ILocalRuleMetadataProvider localRuleMetadataProvider,
            IServerRuleMetadataProvider serverRuleMetadataProvider,
            IConfigurationProvider configurationProvider,
            IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration)
        {
            this.localRuleMetadataProvider = localRuleMetadataProvider;
            this.serverRuleMetadataProvider = serverRuleMetadataProvider;
            this.configurationProvider = configurationProvider;
            this.connectedModeFeaturesConfiguration = connectedModeFeaturesConfiguration;
        }

        public async Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, CancellationToken token)
        {
            var localMetaData = localRuleMetadataProvider.GetRuleInfo(ruleId);

            if (!connectedModeFeaturesConfiguration.IsNewCctAvailable())
            {
                localMetaData = localMetaData.WithCleanCodeTaxonomyDisabled();
            }

            var configuration = configurationProvider.GetConfiguration();

            //TODO: this does not seem to support taint.
            var language = Language.GetLanguageFromRepositoryKey(ruleId.RepoKey);

            // It's possible we'll be asked for help for a language we don't handle locally, in which
            // case we'll return null.
            // See https://github.com/SonarSource/sonarlint-visualstudio/issues/4582
            if (language == null) { return null; }

            ApplicableQualityProfile qualityProfile = null;

            if (!(configuration.Mode.IsInAConnectedMode() && configuration.Project.Profiles.TryGetValue(language, out qualityProfile)))
            {
                return localMetaData;
            }

            var serverMetaData = await serverRuleMetadataProvider.GetRuleInfoAsync(ruleId, qualityProfile.ProfileKey, token);

            if (localMetaData == null)
            {
                return serverMetaData;
            }
  
            if (serverMetaData != null)
            {
                // todo: server overrides for CCT
                return localMetaData.WithServerOverride(serverMetaData.Severity, serverMetaData.HtmlNote);
            }

            return localMetaData;
        }
    }
}
