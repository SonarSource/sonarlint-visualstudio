/*
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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.SLCore.Analysis;

[Export(typeof(IAnalyzer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class SLCoreAnalyzer(
    ISLCoreServiceProvider serviceProvider,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
    ICurrentTimeProvider currentTimeProvider,
    IAggregatingCompilationDatabaseProvider compilationDatabaseLocator,
    IUserSettingsProvider userSettingsProvider,
    ILogger logger)
    : IAnalyzer
{
    private const string CFamilyCompileCommandsProperty = "sonar.cfamily.compile-commands";
    private const string CFamilyReproducerProperty = "sonar.cfamily.reproducer";

    private readonly ILogger cfamilyConfigurationLog = logger.ForContext(SLCoreStrings.SLCoreAnalysisConfigurationLogContext).ForVerboseContext(nameof(EnrichPropertiesForCFamily));

    public void ExecuteAnalysis(
        string path,
        Guid analysisId,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        IAnalyzerOptions analyzerOptions,
        CancellationToken cancellationToken)
    {
        var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), path, analysisId);
        analysisStatusNotifier.AnalysisStarted();

        var configurationScope = activeConfigScopeTracker.Current;
        if (configurationScope is not { IsReadyForAnalysis: true })
        {
            analysisStatusNotifier.AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
            return;
        }

        if (!serviceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService))
        {
            analysisStatusNotifier.AnalysisFailed(SLCoreStrings.ServiceProviderNotInitialized);
            return;
        }

        ExecuteAnalysisInternalAsync(path, configurationScope.Id, analysisId, detectedLanguages, analyzerOptions, analysisService, analysisStatusNotifier, cancellationToken).Forget();
    }

    private async Task ExecuteAnalysisInternalAsync(
        string path,
        string configScopeId,
        Guid analysisId,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        IAnalyzerOptions analyzerOptions,
        IAnalysisSLCoreService analysisService,
        IAnalysisStatusNotifier analysisStatusNotifier,
        CancellationToken cancellationToken)
    {
        try
        {
            EnrichPropertiesForCFamily(out var analysisProperties, path, detectedLanguages, analyzerOptions);

            var stopwatch = Stopwatch.StartNew();

            var (failedAnalysisFiles, _) = await analysisService.AnalyzeFilesAndTrackAsync(
                new AnalyzeFilesAndTrackParams(
                    configScopeId,
                    analysisId,
                    [new FileUri(path)],
                    analysisProperties,
                    analyzerOptions?.IsOnOpen ?? false,
                    currentTimeProvider.Now.ToUnixTimeMilliseconds()),
                cancellationToken);

            if (failedAnalysisFiles.Any())
            {
                analysisStatusNotifier.AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
            }
            else
            {
                analysisStatusNotifier.AnalysisFinished(stopwatch.Elapsed);
            }
        }
        catch (OperationCanceledException)
        {
            analysisStatusNotifier.AnalysisCancelled();
        }
        catch (Exception e)
        {
            analysisStatusNotifier.AnalysisFailed(e);
        }
    }

    private void EnrichPropertiesForCFamily(
        out ImmutableDictionary<string, string> properties,
        string path,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        IAnalyzerOptions analyzerOptions)
    {
        properties = userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties;

        if (!IsCFamily(detectedLanguages))
        {
            return;
        }

        if (analyzerOptions is ICFamilyAnalyzerOptions {CreateReproducer: true})
        {
            properties = properties.SetItem(CFamilyReproducerProperty, path);
        }

        if (properties.TryGetValue(CFamilyCompileCommandsProperty, out var userDefinedCompileCommands))
        {
            cfamilyConfigurationLog.LogVerbose(SLCoreStrings.UserDefinedCompilationDatabase, userDefinedCompileCommands);
            return;
        }

        var compilationDatabase = compilationDatabaseLocator.GetOrNull(path);
        if (compilationDatabase == null)
        {
            cfamilyConfigurationLog.WriteLine(SLCoreStrings.CompilationDatabaseNotFound, path);
            // Pass empty compilation database path in order to get a more helpful message and not break the analyzer
            properties = properties.SetItem(CFamilyCompileCommandsProperty, "");
        }
        else
        {
            properties = properties.SetItem(CFamilyCompileCommandsProperty, compilationDatabase);
        }
    }

    private static bool IsCFamily(IEnumerable<AnalysisLanguage> detectedLanguages) => detectedLanguages != null && detectedLanguages.Contains(AnalysisLanguage.CFamily);
}
