/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.SLCore.Analysis;

[Export(typeof(IAnalyzer))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SLCoreAnalyzer : IAnalyzer
{
    private const string CFamilyCompileCommandsProperty = "sonar.cfamily.compile-commands";

    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
    private readonly ICurrentTimeProvider currentTimeProvider;
    private readonly IAggregatingCompilationDatabaseProvider compilationDatabaseLocator;

    [ImportingConstructor]
    public SLCoreAnalyzer(ISLCoreServiceProvider serviceProvider,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
        ICurrentTimeProvider currentTimeProvider,
        IAggregatingCompilationDatabaseProvider compilationDatabaseLocator)
    {
        this.serviceProvider = serviceProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;
        this.currentTimeProvider = currentTimeProvider;
        this.compilationDatabaseLocator = compilationDatabaseLocator;
    }

    public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
    {
        return true;
    }

    public void ExecuteAnalysis(string path, Guid analysisId, IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConsumer consumer,
        IAnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
    {
        var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), path);
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

        var extraProperties = GetExtraProperties(path, detectedLanguages);

        ExecuteAnalysisInternalAsync(path, configurationScope.Id, analysisId, analyzerOptions, analysisService, analysisStatusNotifier, extraProperties, cancellationToken).Forget();
    }

    private async Task ExecuteAnalysisInternalAsync(string path,
        string configScopeId,
        Guid analysisId,
        IAnalyzerOptions analyzerOptions,
        IAnalysisSLCoreService analysisService,
        IAnalysisStatusNotifier analysisStatusNotifier,
        Dictionary<string, string> extraProperties,
        CancellationToken cancellationToken)
    {
        try
        {
            var (failedAnalysisFiles, _) = await analysisService.AnalyzeFilesAndTrackAsync(
                new AnalyzeFilesAndTrackParams(
                    configScopeId,
                    analysisId,
                    [new FileUri(path)],
                    extraProperties,
                    analyzerOptions?.IsOnOpen ?? false,
                    currentTimeProvider.Now.ToUnixTimeMilliseconds()),
                cancellationToken);

            if (failedAnalysisFiles.Any())
            {
                analysisStatusNotifier.AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
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

    private Dictionary<string, string> GetExtraProperties(string path, IEnumerable<AnalysisLanguage> detectedLanguages)
    {
        Dictionary<string, string> extraProperties = [];
        if (!IsCFamily(detectedLanguages))
        {
            return extraProperties;
        }

        var compilationDatabasePath = compilationDatabaseLocator.GetOrNull(path);
        if (compilationDatabasePath != null)
        {
            extraProperties[CFamilyCompileCommandsProperty] = compilationDatabasePath;
        }
        return extraProperties;
    }

    private static bool IsCFamily(IEnumerable<AnalysisLanguage> detectedLanguages) => detectedLanguages != null && detectedLanguages.Contains(AnalysisLanguage.CFamily);
}
