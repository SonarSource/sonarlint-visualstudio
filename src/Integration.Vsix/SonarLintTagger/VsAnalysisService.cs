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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix;

internal interface IVsAnalysisService
{
    void RequestAnalysis(string filePath, 
        ITextDocument document,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        SnapshotChangedHandler errorListHandler,
        IAnalyzerOptions analyzerOptions);
}

[Export(typeof(IVsAnalysisService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class VsAnalysisService : IVsAnalysisService
{
    private readonly IIssueConsumerFactory issueConsumerFactory;
    private readonly IVsProjectInfoProvider vsProjectInfoProvider;
    private readonly IThreadHandling threadHandling;
    private readonly IAnalyzerController analyzerController;
    private readonly IScheduler scheduler;
    
    internal /* for testing */ const int DefaultAnalysisTimeoutMs = 60 * 1000;

    [ImportingConstructor]
    public VsAnalysisService(IVsProjectInfoProvider vsProjectInfoProvider, 
        IIssueConsumerFactory issueConsumerFactory,
        IAnalyzerController analyzerController,
        IScheduler scheduler, 
        IThreadHandling threadHandling)
    {
        this.issueConsumerFactory = issueConsumerFactory;
        this.vsProjectInfoProvider = vsProjectInfoProvider;
        this.threadHandling = threadHandling;
        this.analyzerController = analyzerController;
        this.scheduler = scheduler;
    }

    public void RequestAnalysis(string filePath, 
        ITextDocument document,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        SnapshotChangedHandler errorListHandler,
        IAnalyzerOptions analyzerOptions)
    {
        RequestAnalysisAsync(filePath, document, detectedLanguages, errorListHandler, analyzerOptions)
            .Forget();
    }

    private async Task RequestAnalysisAsync(string filePath, 
        ITextDocument document, 
        IEnumerable<AnalysisLanguage> detectedLanguages,
        SnapshotChangedHandler errorListHandler,
        IAnalyzerOptions analyzerOptions)
    {
        var (projectName, projectGuid) = await vsProjectInfoProvider.GetDocumentProjectInfoAsync(document);
        var issueConsumer = issueConsumerFactory.Create(document, projectName, projectGuid, errorListHandler);
        
        await ScheduleAnalysisOnBackgroundThreadAsync(filePath, document.Encoding.WebName, detectedLanguages, issueConsumer, analyzerOptions);
    }

    private async Task ScheduleAnalysisOnBackgroundThreadAsync(string filePath,
        string charset,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        IIssueConsumer issueConsumer,
        IAnalyzerOptions analyzerOptions)
    {
        await threadHandling.RunOnBackgroundThread(() =>
        {
            ClearErrorList(filePath, issueConsumer);

            scheduler.Schedule(filePath,
                cancellationToken => analyzerController.ExecuteAnalysis(filePath,
                    charset,
                    detectedLanguages,
                    issueConsumer,
                    analyzerOptions,
                    cancellationToken),
                GetAnalysisTimeoutInMilliseconds());
        });
    }

    private static void ClearErrorList(string filePath, IIssueConsumer issueConsumer)
    {
        issueConsumer.Accept(filePath, Enumerable.Empty<IAnalysisIssue>());
    }
    
    private static int GetAnalysisTimeoutInMilliseconds()
    {
        var environmentSettings = new EnvironmentSettings();
        var userSuppliedTimeout = environmentSettings.AnalysisTimeoutInMs();
        var analysisTimeoutInMilliseconds = userSuppliedTimeout > 0 ? userSuppliedTimeout : DefaultAnalysisTimeoutMs;

        return analysisTimeoutInMilliseconds;
    }
}
