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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.Analysis;

[Export(typeof(IAnalyzer))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SLCoreAnalyzer : IAnalyzer
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;

    [ImportingConstructor]
    public SLCoreAnalyzer(ISLCoreServiceProvider serviceProvider, 
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory)
    {
        this.serviceProvider = serviceProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;
    }

    public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
    {
        return true;
    }

    public void ExecuteAnalysis(string path, Guid analysisId, string charset, IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConsumer consumer,
        IAnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
    {   
        if (!serviceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService))
        {
            throw new NotImplementedException();
        }
        var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), path);

        analysisStatusNotifier.AnalysisStarted();

        if (cancellationToken.IsCancellationRequested)
        {
            analysisStatusNotifier.AnalysisCancelled();
        }

        ExecuteAnalysisInternalAsync(path, analysisId, analysisService, analysisStatusNotifier, cancellationToken).Forget();
    }

    private async Task ExecuteAnalysisInternalAsync(string path, 
        Guid analysisId, 
        IAnalysisSLCoreService analysisService,
        IAnalysisStatusNotifier analysisStatusNotifier,
        CancellationToken cancellationToken)
    {
        try
        {
            var (failedAnalysisFiles, _) = await analysisService.AnalyzeFilesAndTrackAsync(
                new AnalyzeFilesAndTrackParams(
                    activeConfigScopeTracker.Current.Id,
                    analysisId,
                    [new FileUri(path)],
                    [],
                    true,
                    DateTimeOffset.Now.ToUnixTimeMilliseconds()),
                cancellationToken);

            if (failedAnalysisFiles.Any())
            {
                analysisStatusNotifier.AnalysisFailed("Analysis failed.");
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
}
