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
    private IAnalysisSLCoreService? analysisSlCoreService;

    public async Task<Guid?> ExecuteAnalysis(List<string> paths)
    {
        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2049 Pass the analysis ID and the paths to the correct method
        var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), paths[0]);
        analysisStatusNotifier.AnalysisStarted();

        var configurationScope = activeConfigScopeTracker.Current;
        if (!VerifyAnalysisCanBeExecuted(analysisStatusNotifier, configurationScope))
        {
            return null;
        }

        return await ExecuteAnalysisInternalAsync(() => analysisSlCoreService!.AnalyzeFileListAsync(new AnalyzeFileListParams(configurationScope.Id, paths.Select(x => new FileUri(x)).ToList())),
            analysisStatusNotifier);
    }

    public async Task<Guid?> ExecuteAnalysisForOpenFiles()
    {
        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2049 Pass the analysis ID and the paths to the correct method
        var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), null);
        analysisStatusNotifier.AnalysisStarted();

        var configurationScope = activeConfigScopeTracker.Current;
        if (!VerifyAnalysisCanBeExecuted(analysisStatusNotifier, configurationScope))
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

    private bool VerifyAnalysisCanBeExecuted(IAnalysisStatusNotifier analysisStatusNotifier, ConfigurationScope configurationScope)
    {
        if (configurationScope is not { IsReadyForAnalysis: true })
        {
            analysisStatusNotifier.AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
            return false;
        }
        if (analysisSlCoreService != null)
        {
            return true;
        }
        if (serviceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService))
        {
            analysisSlCoreService = analysisService;
            return true;
        }

        analysisStatusNotifier.AnalysisFailed(SLCoreStrings.ServiceProviderNotInitialized);
        return false;
    }

    private static async Task<Guid?> ExecuteAnalysisInternalAsync(
        Func<Task<ForceAnalyzeResponse>> slCoreAnalyzeFilesFunc,
        IAnalysisStatusNotifier analysisStatusNotifier)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var analyzerResponse = await slCoreAnalyzeFilesFunc();

            if (analyzerResponse.analysisId == null)
            {
                analysisStatusNotifier.AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
            }
            else
            {
                analysisStatusNotifier.AnalysisFinished(stopwatch.Elapsed);
            }

            return analyzerResponse.analysisId;
        }
        catch (OperationCanceledException)
        {
            analysisStatusNotifier.AnalysisCancelled();
        }
        catch (Exception e)
        {
            analysisStatusNotifier.AnalysisFailed(e);
        }

        return null;
    }
}
