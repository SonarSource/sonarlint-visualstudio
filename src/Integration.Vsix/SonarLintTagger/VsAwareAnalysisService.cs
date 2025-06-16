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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal record AnalysisSnapshot(string FilePath, ITextSnapshot TextSnapshot);

internal interface IVsAwareAnalysisService
{
    void RequestAnalysis(AnalysisSnapshot analysisSnapshot);
}

[Export(typeof(IVsAwareAnalysisService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class VsAwareAnalysisService(
    IAnalysisService analysisService,
    IThreadHandling threadHandling)
    : IVsAwareAnalysisService
{
    public void RequestAnalysis(AnalysisSnapshot analysisSnapshot) => RequestAnalysisAsync(analysisSnapshot).Forget();

    private async Task RequestAnalysisAsync(AnalysisSnapshot analysisSnapshot)
    {
        await ScheduleAnalysisOnBackgroundThreadAsync(analysisSnapshot.FilePath);
    }

    private async Task ScheduleAnalysisOnBackgroundThreadAsync(string filePath) =>
        await threadHandling.RunOnBackgroundThread(() =>
        {
            analysisService.ScheduleAnalysis(filePath);
        });
}
