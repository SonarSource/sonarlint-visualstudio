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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client;
using SonarQube.Client.Models;
using CoreRuleset = SonarLint.VisualStudio.Core.CSharpVB.RuleSet;
using Language = SonarLint.VisualStudio.Core.Language;
using VsRuleset = Microsoft.VisualStudio.CodeAnalysis.RuleSets.RuleSet;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class DotNetBindingConfigProvider : IBindingConfigProvider
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly INuGetBindingOperation nuGetBindingOperation;
        private readonly BindingConfiguration bindingConfiguration;
        private readonly ILogger logger;
        private readonly ISolutionBindingFilePathGenerator solutionBindingFilePathGenerator;

        public DotNetBindingConfigProvider(ISonarQubeService sonarQubeService, INuGetBindingOperation nuGetBindingOperation, BindingConfiguration bindingConfiguration, ILogger logger)
        : this(sonarQubeService, nuGetBindingOperation, bindingConfiguration, logger, new SolutionBindingFilePathGenerator())
        {
        }

        private readonly IRuleSetGenerator ruleSetGenerator;
        private readonly INuGetPackageInfoGenerator nuGetPackageInfoGenerator;

        public DotNetBindingConfigProvider(ISonarQubeService sonarQubeService, INuGetBindingOperation nuGetBindingOperation, string serverUrl, string projectName, ILogger logger)
            : this(sonarQubeService, nuGetBindingOperation, serverUrl, projectName, logger,
                  new RuleSetGenerator(), new NuGetPackageInfoGenerator())
        {
        }

        internal /* for testing */ DotNetBindingConfigProvider(ISonarQubeService sonarQubeService, INuGetBindingOperation nuGetBindingOperation, string serverUrl, string projectName, ILogger logger,
            IRuleSetGenerator ruleSetGenerator, INuGetPackageInfoGenerator nuGetPackageInfoGenerator)
        {
            this.sonarQubeService = sonarQubeService;
            this.nuGetBindingOperation = nuGetBindingOperation;
            this.bindingConfiguration = bindingConfiguration;
            this.logger = logger;

            this.ruleSetGenerator = ruleSetGenerator;
            this.nuGetPackageInfoGenerator = nuGetPackageInfoGenerator;
        }

        public bool IsLanguageSupported(Language language)
        {
            return Language.CSharp.Equals(language) || Language.VBNET.Equals(language);
        }

        public Task<IBindingConfigFile> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, string UNUSED_organizationKey_TODO_REMOVE, Language language, CancellationToken cancellationToken)
        {
            if (!IsLanguageSupported(language))
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            return DoGetConfigurationAsync(qualityProfile, language, cancellationToken);
        }

        private async Task<IBindingConfigFile> DoGetConfigurationAsync(SonarQubeQualityProfile qualityProfile, Language language, CancellationToken cancellationToken)
        {
            // duncanp - need to change the constructor to pass the project key.
            // To be done after the binding refactoring has been merged to avoid extensive merge conflicts.
            const string TODO_NEED_PROJECT_KEY = null;

            var serverLanguage = language.ServerLanguage;
            Debug.Assert(serverLanguage != null,
                $"Server language should not be null for supported language: {language.Id}");

            // First, fetch the active rules
            var activeRules = await WebServiceHelper.SafeServiceCallAsync(
                () => sonarQubeService.GetRulesAsync(true, qualityProfile.Key, cancellationToken), logger);

            // Give up if the quality profile is empty - not point in fetching anything else
            if (!activeRules.Any())
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, qualityProfile.Name, language.Name)));
                return null;
            }

            // Now fetch the data required for the NuGet configuration
            var sonarProperties = await FetchPropertiesAsync(TODO_NEED_PROJECT_KEY, cancellationToken);

            // Get the NuGet package info and process it if appropriate (only in legacy connected mode, and only C#/VB)
            var nugetInfo = nuGetPackageInfoGenerator.GetNuGetPackageInfos(activeRules, sonarProperties);
            if (!this.nuGetBindingOperation.ProcessExport(language, nugetInfo))
            {
                return null;
            }

            // Finally, fetch the remaining data needed to build the ruleset
            var inactiveRules = await WebServiceHelper.SafeServiceCallAsync(
                () => sonarQubeService.GetRulesAsync(false, qualityProfile.Key, cancellationToken), logger);

            var coreRuleset = CreateRuleset(qualityProfile, language, activeRules.Union(inactiveRules), sonarProperties);

            return new DotNetBindingConfigFile(ToVsRuleset(coreRuleset));
        }

        private async Task<Dictionary<string, string>> FetchPropertiesAsync(string projectKey, CancellationToken cancellationToken)
        {
            var serverProperties = await WebServiceHelper.SafeServiceCallAsync(
                () => sonarQubeService.GetAllPropertiesAsync(projectKey, cancellationToken), logger);

            return serverProperties.ToDictionary(x => x.Key, x => x.Value);
        }

        private RuleSet CreateRuleset(SonarQubeQualityProfile qualityProfile, Language language, IEnumerable<SonarQubeRule> rules, Dictionary<string, string> sonarProperties)
        {
            var coreRuleset = ruleSetGenerator.Generate(language.ServerLanguage.Key, rules, sonarProperties);

            // Set the name and description
            coreRuleset.Name = string.Format(Strings.SonarQubeRuleSetNameFormat, projectName, qualityProfile.Name);
            var descriptionSuffix = string.Format(Strings.SonarQubeQualityProfilePageUrlFormat, serverUrl, qualityProfile.Key);
            coreRuleset.Description = $"{coreRuleset.Description} {descriptionSuffix}";
            return coreRuleset;
        }

        private static VsRuleset ToVsRuleset(CoreRuleset coreRuleset)
        {
            // duncanp - refactor so IBindingConfigFileWithRuleset the VS RuleSet is not used
            // (looks like only the ruleset DisplayName used by consumers of IBindingConfigFileWithRuleset
            // so we don't actually need a ruleset)
            var tempRuleSetFilePath = Path.GetTempFileName();
            File.WriteAllText(tempRuleSetFilePath, coreRuleset.ToXml());
            var ruleSet = VsRuleset.LoadFromFile(tempRuleSetFilePath);
            
            return ruleSet;
        }
    }
}
