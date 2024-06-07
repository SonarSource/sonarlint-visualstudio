/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.Analysis;

/// <summary>
/// Maintains analysis and issue processing
/// </summary>
public interface IAnalysisService
{
    /// <summary>
    /// Indicates whether at least one language from <paramref name="languages"/> list is analyzable.
    /// </summary>
    bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages);

    /// <summary>
    /// Handles analysis results
    /// </summary>
    void PublishIssues(Guid analysisId, IEnumerable<IAnalysisIssue> issues);

    /// <summary>
    /// Starts analysis for <paramref name="filePath"/>
    /// </summary>
    void ScheduleAnalysis(string filePath,
        Guid analysisId,
        string charset,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        IIssueConsumer issueConsumer,
        IAnalyzerOptions analyzerOptions);

    /// <summary>
    /// Stops issue publishing for <paramref name="filePath"/> until the next <see cref="ScheduleAnalysis"/> is called
    /// </summary>
    void CancelForFile(string filePath);
}
