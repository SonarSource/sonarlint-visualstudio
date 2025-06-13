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

namespace SonarLint.VisualStudio.Core.Analysis;

[Export(typeof(IAnalysisService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AnalysisService : IAnalysisService
{
    internal /* for testing */ const int DefaultAnalysisTimeoutMs = 60 * 1000;

    private readonly IAnalyzer analyzer;
    private readonly IScheduler scheduler;

    [ImportingConstructor]
    internal AnalysisService(IAnalyzer analyzer, IScheduler scheduler)
    {
        this.analyzer = analyzer;
        this.scheduler = scheduler;
    }

    public void ScheduleAnalysis(string filePath)
    {
        scheduler.Schedule(filePath,
            token =>
            {
                if (!token.IsCancellationRequested)
                {
                    analyzer.ExecuteAnalysis([filePath]);
                }
            },
            GetAnalysisTimeoutInMilliseconds());
    }

    private static int GetAnalysisTimeoutInMilliseconds()
    {
        var environmentSettings = new EnvironmentSettings();
        var userSuppliedTimeout = environmentSettings.AnalysisTimeoutInMs();

        return userSuppliedTimeout > 0 ? userSuppliedTimeout : DefaultAnalysisTimeoutMs;
    }
}
