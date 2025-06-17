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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.TaskProgress;

namespace SonarLint.VisualStudio.SLCore.Analysis;

[Export(typeof(IAnalyzer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class SLCoreAnalyzer(
    ISLCoreServiceProvider serviceProvider,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
    ILogger logger)
    : IAnalyzer
{
    private readonly ILogger logger = logger.ForContext(nameof(SLCoreAnalyzer));

    public async Task<Guid?> ExecuteAnalysis(List<string> paths)
    {
        var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), paths.ToArray());
        analysisStatusNotifier.AnalysisStarted();

        var configurationScope = activeConfigScopeTracker.Current;
        if (!VerifyConfigScopeInitialized(analysisStatusNotifier, configurationScope) ||
            GetAnalysisSlCoreService(analysisStatusNotifier) is not { } analysisSlCoreService)
        {
            return null;
        }

        return await ExecuteAnalysisInternalAsync(() => analysisSlCoreService!.AnalyzeFileListAsync(new AnalyzeFileListParams(configurationScope.Id, paths.Select(x => new FileUri(x)).ToList())),
            analysisStatusNotifier);
    }

    public async Task<Guid?> ExecuteAnalysisForOpenFiles()
    {
        var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer));
        analysisStatusNotifier.AnalysisStarted();

        var configurationScope = activeConfigScopeTracker.Current;
        if (!VerifyConfigScopeInitialized(analysisStatusNotifier, configurationScope) ||
            GetAnalysisSlCoreService(analysisStatusNotifier) is not { } analysisSlCoreService)
        {
            return null;
        }

        return await ExecuteAnalysisInternalAsync(() => analysisSlCoreService!.AnalyzeOpenFilesAsync(new AnalyzeOpenFilesParams(configurationScope.Id)), analysisStatusNotifier);
    }

    public void CancelAnalysis(Guid analysisId)
    {
        if (!serviceProvider.TryGetTransientService(out ITaskProgressSLCoreService taskProgressSlCoreService))
        {
            logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
            return;
        }

        taskProgressSlCoreService.CancelTask(new CancelTaskParams(analysisId.ToString()));
        logger.WriteLine(SLCoreStrings.AnalysisCancelled, analysisId);
    }

    private static bool VerifyConfigScopeInitialized(IAnalysisStatusNotifier analysisStatusNotifier, ConfigurationScope configurationScope)
    {
        if (configurationScope is { IsReadyForAnalysis: true })
        {
            return true;
        }

        analysisStatusNotifier.AnalysisNotReady(analysisId: null, SLCoreStrings.ConfigScopeNotInitialized);
        return false;
    }

    private IAnalysisSLCoreService? GetAnalysisSlCoreService(IAnalysisStatusNotifier analysisStatusNotifier)
    {
        if (serviceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService))
        {
            return analysisService;
        }

        analysisStatusNotifier.AnalysisFailed(analysisId: null, SLCoreStrings.ServiceProviderNotInitialized);
        return null;
    }

    private static async Task<Guid?> ExecuteAnalysisInternalAsync(
        Func<Task<ForceAnalyzeResponse>> slCoreAnalyzeFilesFunc,
        IAnalysisStatusNotifier analysisStatusNotifier)
    {
        ForceAnalyzeResponse? analyzerResponse = null;
        try
        {
            var stopwatch = Stopwatch.StartNew();
            analyzerResponse = await slCoreAnalyzeFilesFunc();

            if (analyzerResponse.analysisId == null)
            {
                analysisStatusNotifier.AnalysisFailed(analyzerResponse.analysisId, SLCoreStrings.AnalysisFailedReason);
            }
            else
            {
                analysisStatusNotifier.AnalysisFinished(analyzerResponse.analysisId, stopwatch.Elapsed);
            }

            return analyzerResponse.analysisId;
        }
        catch (OperationCanceledException)
        {
            analysisStatusNotifier.AnalysisCancelled(analysisId: analyzerResponse?.analysisId);
        }
        catch (Exception e)
        {
            analysisStatusNotifier.AnalysisFailed(analysisId: analyzerResponse?.analysisId, e);
        }

        return null;
    }
}
