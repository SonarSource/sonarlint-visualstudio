﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using System.IO;
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.ConnectedMode.Binding;

/// <summary>
/// Turn server Quality Profile information into a set of config files for a particular language
/// </summary>
[Export(typeof(IBindingConfigProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynBindingConfigProvider(
    ISonarQubeService sonarQubeService,
    ILogger logger,
    IGlobalConfigGenerator globalConfigGenerator,
    ISonarLintConfigGenerator sonarLintConfigGenerator,
    ILanguageProvider languageProvider)
    : IBindingConfigProvider
{
    private const string TaintAnalysisRepoPrefix = "roslyn.sonaranalyzer.security.";
    private readonly IEnvironmentSettings environmentSettings = new EnvironmentSettings();

    public bool IsLanguageSupported(Language language) => languageProvider.RoslynLanguages.Contains(language);

    public Task<IBindingConfig> GetConfigurationAsync(
        SonarQubeQualityProfile qualityProfile,
        Language language,
        BindingConfiguration bindingConfiguration,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        return DoGetConfigurationAsync(qualityProfile, language, bindingConfiguration, cancellationToken);
    }

    private async Task<IBindingConfig> DoGetConfigurationAsync(
        SonarQubeQualityProfile qualityProfile,
        Language language,
        BindingConfiguration bindingConfiguration,
        CancellationToken cancellationToken)
    {
        var serverLanguageKey = language.ServerLanguageKey;
        Debug.Assert(serverLanguageKey != null,
            $"Server language should not be null for supported language: {language.Id}");

        // First, fetch the active rules
        var activeRules = await FetchSupportedRulesAsync(true, qualityProfile.Key, cancellationToken);

        // Give up if the quality profile is empty - no point in fetching anything else
        if (!activeRules.Any())
        {
            logger.WriteLine(string.Format(BindingStrings.SubTextPaddingFormat,
                string.Format(BindingStrings.NoSonarAnalyzerActiveRulesForQualityProfile, qualityProfile.Name, language.Name)));
            return null;
        }

        // Now fetch the data required for the NuGet configuration
        var sonarProperties = await FetchPropertiesAsync(bindingConfiguration.Project.ServerProjectKey, cancellationToken);

        // Finally, fetch the remaining data needed to build the globalconfig
        var inactiveRules = await FetchSupportedRulesAsync(false, qualityProfile.Key, cancellationToken);
        var exclusions = await FetchInclusionsExclusionsAsync(bindingConfiguration.Project.ServerProjectKey, cancellationToken);

        var globalConfig = GetGlobalConfig(language, bindingConfiguration, activeRules, inactiveRules);
        var additionalFile = GetAdditionalFile(language, bindingConfiguration, activeRules, sonarProperties, exclusions);

        return new CSharpVBBindingConfig(globalConfig, additionalFile);
    }

    private async Task<ServerExclusions> FetchInclusionsExclusionsAsync(
        string projectKey,
        CancellationToken cancellationToken)
    {
        var exclusions = await WebServiceHelper.SafeServiceCallAsync(
            () => sonarQubeService.GetServerExclusions(projectKey, cancellationToken), logger);

        return exclusions;
    }

    private FilePathAndContent<string> GetGlobalConfig(
        Language language,
        BindingConfiguration bindingConfiguration,
        IEnumerable<SonarQubeRule> activeRules,
        IEnumerable<SonarQubeRule> inactiveRules)
    {
        var globalConfig = globalConfigGenerator.Generate(activeRules.Union(inactiveRules).Select(x => new SonarQubeRoslynRuleStatus(x, environmentSettings)));

        var globalConfigFilePath = GetSolutionGlobalConfigFilePath(language, bindingConfiguration);

        return new FilePathAndContent<string>(globalConfigFilePath, globalConfig);
    }

    private FilePathAndContent<SonarLintConfiguration> GetAdditionalFile(
        Language language,
        BindingConfiguration bindingConfiguration,
        IEnumerable<SonarQubeRule> activeRules,
        IDictionary<string, string> sonarProperties,
        ServerExclusions serverExclusions)
    {
        var additionalFilePath = GetSolutionAdditionalFilePath(language, bindingConfiguration);
        var additionalFileContent = sonarLintConfigGenerator.Generate(activeRules, sonarProperties, serverExclusions, language);

        var additionalFile = new FilePathAndContent<SonarLintConfiguration>(additionalFilePath, additionalFileContent);

        return additionalFile;
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

    internal static /* for testing */ bool IsSupportedRule(SonarQubeRule rule)
    {
        // We don't want to generate configuration for taint-analysis rules or hotspots.
        // * taint-analysis rules: these are in a separate analyzer that doesn't ship in SLVS so there is no point in generating config
        // * hotspots: these are noisy so we don't want to run them in the IDE. There is special code in the Sonar hotspot analyzers to
        //              control when they run; we are responsible for not generating configuration for them.
        return IsSupportedIssueType(rule.IssueType) && !IsTaintAnalysisRule(rule);
    }

    private static bool IsTaintAnalysisRule(SonarQubeRule rule) => rule.RepositoryKey.StartsWith(TaintAnalysisRepoPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedIssueType(SonarQubeIssueType issueType) =>
        issueType == SonarQubeIssueType.CodeSmell ||
        issueType == SonarQubeIssueType.Bug ||
        issueType == SonarQubeIssueType.Vulnerability;

    internal static string GetSolutionGlobalConfigFilePath(Language language, BindingConfiguration bindingConfiguration)
    {
        return bindingConfiguration.BuildPathUnderConfigDirectory(language.SettingsFileNameAndExtension);
    }

    internal static string GetSolutionAdditionalFilePath(Language language, BindingConfiguration bindingConfiguration)
    {
        var additionalFilePathDirectory = bindingConfiguration.BuildPathUnderConfigDirectory();

        return Path.Combine(additionalFilePathDirectory, language.Id, "SonarLint.xml");
    }
}
