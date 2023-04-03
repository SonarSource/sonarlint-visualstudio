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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Rules
{
    /// <summary>
    /// Returns rule metadata
    /// </summary>
    /// <remarks>
    /// The metadata is received from SQ Client
    /// </remarks>
    public interface IServerRuleMetadataProvider
    {
        /// <summary>
        /// Fetches rule info for the specified language rule
        /// </summary>
        /// <returns>The rule info, or null if the repo key/rule was not recognised</returns>
        Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, string qualityProfileKey, CancellationToken token);
    }

    [Export(typeof(IServerRuleMetadataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ServerRuleMetadataProvider : IServerRuleMetadataProvider
    {
        private readonly ISonarQubeService service;
        private readonly ILogger logger;

        [ImportingConstructor]
        public ServerRuleMetadataProvider(ISonarQubeService service, ILogger logger)
        {
            this.service = service;
            this.logger = logger;
        }

        public async Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, string qualityProfileKey, CancellationToken token)
        {
            SonarQubeRule sqRule = null;
            try
            {
                sqRule = await service.GetRuleByKeyAsync(ruleId.ToString(), qualityProfileKey, token);
            }
            catch (Exception ex)
            {
                logger.WriteLine(Resources.ServerMetadataProvider_GetRulesError, ruleId.ToString(), ex.Message);
            }
            if (sqRule == null) return null;

            var descriptionSections = sqRule.DescriptionSections?.Select(ds => ds.ToDescriptionSection()).ToList();

            return new RuleInfo(sqRule.RepositoryKey,
                sqRule.GetCompositeKey(),
                HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(sqRule.Description),
                sqRule.Name,
                sqRule.Severity.ToRuleIssueSeverity(),
                sqRule.IssueType.ToRuleIssueType(),
                sqRule.IsActive,
                sqRule.Tags,
                descriptionSections,
                sqRule.EducationPrinciples,
                HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(sqRule.HtmlNote));
        }
    }
}
