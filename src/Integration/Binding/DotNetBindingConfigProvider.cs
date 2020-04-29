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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class DotNetBindingConfigProvider : IBindingConfigProvider
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly INuGetBindingOperation nuGetBindingOperation;
        private readonly ILogger logger;

        private readonly string serverUrl;
        private readonly string projectName;

        public DotNetBindingConfigProvider(ISonarQubeService sonarQubeService, INuGetBindingOperation nuGetBindingOperation, string serverUrl, string projectName, ILogger logger)
        {
            this.sonarQubeService = sonarQubeService;
            this.nuGetBindingOperation = nuGetBindingOperation;
            this.serverUrl = serverUrl;
            this.projectName = projectName;
            this.logger = logger;
        }

        public bool IsLanguageSupported(Language language)
        {
            return Language.CSharp.Equals(language) || Language.VBNET.Equals(language);
        }

        public async Task<IBindingConfigFile> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, string organizationKey, Language language, CancellationToken cancellationToken)
        {
            if(!IsLanguageSupported(language))
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            var serverLanguage = language.ServerLanguage;
            Debug.Assert(serverLanguage != null,
                $"Server language should not be null for supported language: {language.Id}");

            // Generate the rules configuration file for the language
            var roslynProfileExporter = await WebServiceHelper.SafeServiceCallAsync(() =>
                this.sonarQubeService.GetRoslynExportProfileAsync(qualityProfile.Name,
                    organizationKey, serverLanguage, cancellationToken),
                this.logger);
            if (roslynProfileExporter == null)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadFailedMessageFormat, qualityProfile.Name,
                        qualityProfile.Key, language.Name)));
                return null;
            }

            var tempRuleSetFilePath = Path.GetTempFileName();
            File.WriteAllText(tempRuleSetFilePath, roslynProfileExporter.Configuration.RuleSet.OuterXml);
            RuleSet ruleSet = RuleSet.LoadFromFile(tempRuleSetFilePath);

            // Give up if the quality profile is empty
            if (ruleSet == null ||
                ruleSet.Rules.Count == 0 ||
                ruleSet.Rules.All(rule => rule.Action == RuleAction.None))
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, qualityProfile.Name, language.Name)));
                return null;
            }

            // duncanp - WIP - next line will be removed when the RoslynProfileExporter is removed
            var nugetPackages = roslynProfileExporter.Deployment.NuGetPackages.Select(x => new Core.CSharpVB.NuGetPackageInfo(x.Id, x.Version));

            // Add the NuGet reference, if appropriate (only in legacy connected mode, and only C#/VB)
            if (!this.nuGetBindingOperation.ProcessExport(language, nugetPackages))
            {
                return null;
            }

            // Remove/Move/Refactor code when XML ruleset file is no longer downloaded but the proper API is used to retrieve rules
            UpdateDownloadedSonarQubeQualityProfile(ruleSet, qualityProfile, this.projectName, this.serverUrl);

            return new DotNetBindingConfigFile(ruleSet);
        }

        private void UpdateDownloadedSonarQubeQualityProfile(RuleSet ruleSet, SonarQubeQualityProfile qualityProfile, string projectName, string serverUrl)
        {
            ruleSet.NonLocalizedDisplayName = string.Format(Strings.SonarQubeRuleSetNameFormat, projectName, qualityProfile.Name);

            var ruleSetDescriptionBuilder = new StringBuilder();
            ruleSetDescriptionBuilder.AppendLine(ruleSet.Description);
            ruleSetDescriptionBuilder.AppendFormat(Strings.SonarQubeQualityProfilePageUrlFormat, serverUrl, qualityProfile.Key);
            ruleSet.NonLocalizedDescription = ruleSetDescriptionBuilder.ToString();

            ruleSet.WriteToFile(ruleSet.FilePath);
        }
    }
}
