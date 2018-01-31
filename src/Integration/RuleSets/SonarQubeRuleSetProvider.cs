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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Messages;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.RuleSets
{
    internal class SonarQubeRuleSetProvider : IRuleSetProvider
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;

        public SonarQubeRuleSetProvider(ISonarQubeService sonarQubeService, ILogger logger)
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

        public SonarRuleSet GetRuleSet(BoundSonarQubeProject project, Language language)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            if (language != Language.CSharp && language != Language.VBNET)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            var roslynProfileExporter = GetSonarQubeRuleSet(project, language).GetAwaiter().GetResult();
            if (roslynProfileExporter == null)
            {
                return null;
            }

            var ruleset = ConvertToCodeAnalysisRuleSet(roslynProfileExporter);

            return ConvertToSonarRuleSet(ruleset, language);
        }

        private async Task<RoslynExportProfileResponse> GetSonarQubeRuleSet(BoundSonarQubeProject project, Language language)
        {
            var serverLanguage = language.ToServerLanguage();

            var qualityProfileInfo = await SafeServiceCall(() => this.sonarQubeService.GetQualityProfileAsync(
                project.ProjectKey, project.Organization?.Key, serverLanguage, CancellationToken.None));
            if (qualityProfileInfo == null)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                   string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name)));
                return null;
            }

            var roslynProfileExporter = await SafeServiceCall(() => this.sonarQubeService.GetRoslynExportProfileAsync(
                qualityProfileInfo.Name, project.Organization?.Key, serverLanguage, CancellationToken.None));
            if (roslynProfileExporter == null)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadFailedMessageFormat, qualityProfileInfo.Name,
                        qualityProfileInfo.Key, language.Name)));
                return null;
            }

            return roslynProfileExporter;
        }

        private async Task<T> SafeServiceCall<T>(Func<Task<T>> call)
        {
            try
            {
                return await call();
            }
            catch (HttpRequestException e)
            {
                // For some errors we will get an inner exception which will have a more specific information
                // that we would like to show i.e.when the host could not be resolved
                var innerException = e.InnerException as System.Net.WebException;
                this.logger.WriteLine(Strings.SonarQubeRequestFailed, e.Message, innerException?.Message);
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                this.logger.WriteLine(Strings.SonarQubeRequestTimeoutOrCancelled);
            }
            catch (Exception ex)
            {
                this.logger.WriteLine(Strings.SonarQubeRequestFailed, ex.Message, null);
            }

            return default(T);
        }

        private RuleSet ConvertToCodeAnalysisRuleSet(RoslynExportProfileResponse roslynProfileExporter)
        {
            var tempRuleSetFilePath = Path.GetTempFileName();
            File.WriteAllText(tempRuleSetFilePath, roslynProfileExporter.Configuration.RuleSet.OuterXml);

            return RuleSet.LoadFromFile(tempRuleSetFilePath);
        }

        private SonarRuleSet ConvertToSonarRuleSet(RuleSet ruleset, Language language)
        {
            var rules = ruleset.Rules.Select(r => new SonarRule(r.RuleInfo.AnalyzerId, ConvertToIdeSeverity(r.Action)));

            return new SonarRuleSet(ruleset.DisplayName, language, rules);
        }

        private RuleIdeSeverity ConvertToIdeSeverity(RuleAction ruleAction)
        {
            switch (ruleAction)
            {
                case RuleAction.Error:
                    return RuleIdeSeverity.Error;
                case RuleAction.Warning:
                    return RuleIdeSeverity.Warning;
                case RuleAction.Info:
                    return RuleIdeSeverity.Info;
                case RuleAction.Hidden:
                    return RuleIdeSeverity.Hidden;
                default:
                    return RuleIdeSeverity.Disabled;
            }
        }
    }
}
