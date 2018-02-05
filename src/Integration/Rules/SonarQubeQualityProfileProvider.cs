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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Messages;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Rules
{
    public class SonarQubeQualityProfileProvider : IQualityProfileProvider
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;

        public SonarQubeQualityProfileProvider(ISonarQubeService sonarQubeService, ILogger logger)
        {
            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.sonarQubeService = sonarQubeService;
            this.logger = logger;
        }

        public QualityProfile GetQualityProfile(BoundSonarQubeProject project, Language language)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            // For now only C# and VB.Net have support for the connected mode in SLVS
            if (language != Language.CSharp &&
                language != Language.VBNET)
            {
                return null;
            }

            var roslynProfileExporter = GetSonarQubeQualityProfile(project, language).GetAwaiter().GetResult();
            if (roslynProfileExporter == null)
            {
                return null;
            }

            var ruleset = ConvertToCodeAnalysisRuleSet(roslynProfileExporter);

            return ConvertToSonarLintQualityProfile(ruleset, language);
        }

        private async Task<RoslynExportProfileResponse> GetSonarQubeQualityProfile(BoundSonarQubeProject project, Language language)
        {
            var serverLanguage = language.ToServerLanguage();

            var qualityProfileInfo = await WebServiceHelper.SafeServiceCall(
                () => this.sonarQubeService.GetQualityProfileAsync(project.ProjectKey, project.Organization?.Key, serverLanguage,
                    CancellationToken.None),
                this.logger);
            if (qualityProfileInfo == null)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                   string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name)));
                return null;
            }

            var roslynProfileExporter = await WebServiceHelper.SafeServiceCall(
                () => this.sonarQubeService.GetRoslynExportProfileAsync(qualityProfileInfo.Name, project.Organization?.Key,
                    serverLanguage, CancellationToken.None),
                this.logger);
            if (roslynProfileExporter == null)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadFailedMessageFormat, qualityProfileInfo.Name,
                        qualityProfileInfo.Key, language.Name)));
                return null;
            }

            return roslynProfileExporter;
        }

        private RuleSet ConvertToCodeAnalysisRuleSet(RoslynExportProfileResponse roslynProfileExporter)
        {
            RuleSet ruleSet = null;

            try
            {
                var tempRuleSetFilePath = Path.GetTempFileName();

                File.WriteAllText(tempRuleSetFilePath, roslynProfileExporter.Configuration.RuleSet.OuterXml);
                ruleSet = RuleSet.LoadFromFile(tempRuleSetFilePath);
                File.Delete(tempRuleSetFilePath);
            }
            catch
            {
                // Do nothing
            }

            return ruleSet;
        }

        private QualityProfile ConvertToSonarLintQualityProfile(RuleSet ruleset, Language language)
        {
            // The ruleset exporter doesn't provide all rules with their activation status. Instead it returns rules activated
            // that are not part of SonarWay and rules disabled that are in SonarWay.
            // Hence the decision to take only activated rules (the manager will assume it is disabled by default).
            var rules = ruleset.Rules.Where(r => r.Action != RuleAction.None).Select(r => new SonarRule(r.RuleInfo.AnalyzerId));

            return new QualityProfile(language, rules);
        }
    }
}
