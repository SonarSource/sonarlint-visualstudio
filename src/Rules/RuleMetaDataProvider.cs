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

namespace SonarLint.VisualStudio.Rules
{
    public interface IRuleMetaDataProvider
    {
        Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, CancellationToken token);
    }

    [Export(typeof(IRuleMetaDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RuleMetaDataProvider : IRuleMetaDataProvider
    {
        private readonly ILocalRuleMetadataProvider localRuleMetadataProvider;
        private readonly IServerRuleMetadataProvider serverRuleMetadataProvider;
        private readonly IConfigurationProvider configurationProvider;

        [ImportingConstructor]
        public RuleMetaDataProvider(ILocalRuleMetadataProvider localRuleMetadataProvider, IServerRuleMetadataProvider serverRuleMetadataProvider, IConfigurationProvider configurationProvider)
        {
            this.localRuleMetadataProvider = localRuleMetadataProvider;
            this.serverRuleMetadataProvider = serverRuleMetadataProvider;
            this.configurationProvider = configurationProvider;
        }

        public async Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, CancellationToken token)
        {
            var localMetaData = localRuleMetadataProvider.GetRuleInfo(ruleId);

            var configuration = configurationProvider.GetConfiguration();

            //TODO: this does not seem to support taint.
            var language = Language.GetLanguageFromRepositoryKey(ruleId.RepoKey);
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
                localMetaData.DefaultSeverity = serverMetaData.DefaultSeverity;
                localMetaData.HtmlNote = serverMetaData.HtmlNote;
            }

            return localMetaData;
        }
    }
}
