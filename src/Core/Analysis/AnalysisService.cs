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

namespace SonarLint.VisualStudio.Core.Analysis;

[Export(typeof(IAnalysisService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AnalysisService : IAnalysisService
{
    internal /* for testing */ const int DefaultAnalysisTimeoutMs = 60 * 1000;

    private readonly IAnalyzerController analyzerController;
    private readonly IIssueConsumerStorage issueConsumerStorage;
    private readonly IScheduler scheduler;

    [ImportingConstructor]
    internal AnalysisService(IAnalyzerController analyzerController, IIssueConsumerStorage issueConsumerStorage, IScheduler scheduler)
    {
        this.analyzerController = analyzerController;
        this.issueConsumerStorage = issueConsumerStorage;
        this.scheduler = scheduler;
    }

    public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
    {
        return analyzerController.IsAnalysisSupported(languages);
    }

    public void PublishIssues(string filePath, Guid analysisId, IEnumerable<IAnalysisIssue> issues)
    {
        if (issueConsumerStorage.TryGet(filePath, out var currentAnalysisId, out var issueConsumer)
            && analysisId == currentAnalysisId)
        {
            issueConsumer.Accept(filePath, issues);
        }
    }

    public void ScheduleAnalysis(string filePath,
        Guid analysisId,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        IIssueConsumer issueConsumer,
        IAnalyzerOptions analyzerOptions)
    {
        scheduler.Schedule(filePath,
            token =>
            {
                if (!token.IsCancellationRequested)
                {
                    issueConsumerStorage.Set(filePath, analysisId, issueConsumer);
                    analyzerController.ExecuteAnalysis(filePath, analysisId, detectedLanguages, issueConsumer, analyzerOptions, token);
                }
            },
            GetAnalysisTimeoutInMilliseconds());
    }

    public void CancelForFile(string filePath)
    {
        scheduler.Schedule(filePath,
            token =>
            {
                if (!token.IsCancellationRequested)
                {
                    issueConsumerStorage.Remove(filePath);
                }
            },
            -1);
    }
    
    private static int GetAnalysisTimeoutInMilliseconds()
    {
        var environmentSettings = new EnvironmentSettings();
        var userSuppliedTimeout = environmentSettings.AnalysisTimeoutInMs();

        return userSuppliedTimeout > 0 ? userSuppliedTimeout : DefaultAnalysisTimeoutMs;
    }
}
