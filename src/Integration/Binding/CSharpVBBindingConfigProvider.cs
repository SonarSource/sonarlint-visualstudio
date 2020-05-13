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
    internal class CSharpVBBindingConfigProvider : IBindingConfigProvider
    {
        private const string TaintAnalyisRepoPrefix = "roslyn.sonaranalyzer.security.";

        private readonly ISonarQubeService sonarQubeService;
        private readonly INuGetBindingOperation nuGetBindingOperation;
        private readonly ILogger logger;
        private readonly IRuleSetGenerator ruleSetGenerator;
        private readonly INuGetPackageInfoGenerator nuGetPackageInfoGenerator;
        private readonly ISolutionBindingFilePathGenerator solutionBindingFilePathGenerator;

        public CSharpVBBindingConfigProvider(ISonarQubeService sonarQubeService, INuGetBindingOperation nuGetBindingOperation, ILogger logger)
            : this(sonarQubeService, nuGetBindingOperation, logger,
                  new RuleSetGenerator(), new NuGetPackageInfoGenerator(), new SolutionBindingFilePathGenerator())
        {
        }

        internal /* for testing */ CSharpVBBindingConfigProvider(ISonarQubeService sonarQubeService,
            INuGetBindingOperation nuGetBindingOperation, ILogger logger,
            IRuleSetGenerator ruleSetGenerator, INuGetPackageInfoGenerator nuGetPackageInfoGenerator,
            ISolutionBindingFilePathGenerator solutionBindingFilePathGenerator)
        {
            this.sonarQubeService = sonarQubeService;
            this.nuGetBindingOperation = nuGetBindingOperation;
            this.logger = logger;
            this.ruleSetGenerator = ruleSetGenerator;
            this.nuGetPackageInfoGenerator = nuGetPackageInfoGenerator;
            this.solutionBindingFilePathGenerator = solutionBindingFilePathGenerator;
        }

        public bool IsLanguageSupported(Language language)
        {
            return Language.CSharp.Equals(language) || Language.VBNET.Equals(language);
        }

        public Task<IBindingConfig> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, Language language, BindingConfiguration bindingConfiguration, CancellationToken cancellationToken)
        {
            if (!IsLanguageSupported(language))
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            return DoGetConfigurationAsync(qualityProfile, language, bindingConfiguration, cancellationToken);
        }

        private async Task<IBindingConfig> DoGetConfigurationAsync(SonarQubeQualityProfile qualityProfile, Language language, BindingConfiguration bindingConfiguration, CancellationToken cancellationToken)
        {
            var serverLanguage = language.ServerLanguage;
            Debug.Assert(serverLanguage != null,
                $"Server language should not be null for supported language: {language.Id}");

            // First, fetch the active rules
            var activeRules = await FetchSupportedRulesAsync(true, qualityProfile.Key, cancellationToken);

            // Give up if the quality profile is empty - no point in fetching anything else
            if (!activeRules.Any())
            {
                logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, qualityProfile.Name, language.Name)));
                return null;
            }

            // Now fetch the data required for the NuGet configuration
            var sonarProperties = await FetchPropertiesAsync(bindingConfiguration.Project.ProjectKey, cancellationToken);

            // Get the NuGet package info and process it if appropriate (only in legacy connected mode, and only C#/VB)
            var nugetInfo = nuGetPackageInfoGenerator.GetNuGetPackageInfos(activeRules, sonarProperties);
            if (!nuGetBindingOperation.ProcessExport(language, nugetInfo))
            {
                return null;
            }

            // Finally, fetch the remaining data needed to build the ruleset
            var inactiveRules = await FetchSupportedRulesAsync(false, qualityProfile.Key, cancellationToken);

            var coreRuleset = CreateRuleset(qualityProfile, language, bindingConfiguration, activeRules.Union(inactiveRules), sonarProperties);

            var ruleSetFilePath = solutionBindingFilePathGenerator.Generate(
                bindingConfiguration.BindingConfigDirectory,
                bindingConfiguration.Project.ProjectKey,
                language.FileSuffixAndExtension);

            return new CSharpVBBindingConfig(ToVsRuleset(coreRuleset), ruleSetFilePath);
        }

        private async Task<IEnumerable<SonarQubeRule>> FetchSupportedRulesAsync(bool active, string qpKey, CancellationToken cancellationToken)
        {
            var rules = await WebServiceHelper.SafeServiceCallAsync(
                () => sonarQubeService.GetRulesAsync(active, qpKey, cancellationToken), logger);
            return rules.Where(IsSupportedRule).ToArray();
        }

        private async Task<Dictionary<string, string>> FetchPropertiesAsync(string projectKey, CancellationToken cancellationToken)
        {
            var serverProperties = await WebServiceHelper.SafeServiceCallAsync(
                () => sonarQubeService.GetAllPropertiesAsync(projectKey, cancellationToken), logger);

            return serverProperties.ToDictionary(x => x.Key, x => x.Value);
        }

        private CoreRuleset CreateRuleset(SonarQubeQualityProfile qualityProfile, Language language, BindingConfiguration bindingConfiguration, IEnumerable<SonarQubeRule> rules, Dictionary<string, string> sonarProperties)
        {
            var coreRuleset = ruleSetGenerator.Generate(language.ServerLanguage.Key, rules, sonarProperties);

            // Set the name and description
            coreRuleset.Name = string.Format(Strings.SonarQubeRuleSetNameFormat, bindingConfiguration.Project.ProjectName, qualityProfile.Name);
            var descriptionSuffix = string.Format(Strings.SonarQubeQualityProfilePageUrlFormat, bindingConfiguration.Project.ServerUri, qualityProfile.Key);
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

        internal static  /* for testing */ bool IsSupportedRule(SonarQubeRule rule)
        {
            // We don't want to generate configuration for taint-analysis rules or hotspots.
            // * taint-analysis rules: these are in a separate analyzer that doesn't ship in SLVS so there is no point in generating config
            // * hotspots: these are noisy so we don't want to run them in the IDE. There is special code in the Sonar hotspot analyzers to 
            //              control when they run; we are responsible for not generating configuration for them.

            // duncanp: TODO - exclude hotspots. The SonarQubeRule doesn't currently contain sufficient data for us to do this.
            return !rule.RepositoryKey.StartsWith(TaintAnalyisRepoPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
