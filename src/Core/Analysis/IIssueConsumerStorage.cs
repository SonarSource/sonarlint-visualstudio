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

namespace SonarLint.VisualStudio.Core.Analysis;

/// <summary>
/// Storage of <see cref="IIssueConsumer"/> for all currently scheduled analyses
/// </summary>
public interface IIssueConsumerStorage
{
    /// <summary>
    /// Sets the latest <paramref name="analysisId"/> for <paramref name="filePath"/> and associated <paramref name="issueConsumer"/>
    /// </summary>
    /// <param name="filePath">File system path for which the analysis was scheduled</param>
    /// <param name="analysisId">Unique analysis identifier</param>
    /// <param name="issueConsumer">Consumer for analysis results</param>
    void Set(string filePath, Guid analysisId, IIssueConsumer issueConsumer);
    /// <summary>
    /// Gets the latest <paramref name="analysisId"/> for <paramref name="filePath"/> and associated <paramref name="issueConsumer"/>
    /// </summary>
    /// <param name="filePath">File system path for which the analysis was scheduled</param>
    /// <param name="analysisId">Unique analysis identifier</param>
    /// <param name="issueConsumer">Consumer for analysis results</param>
    /// <returns>true if analysis is scheduled for the given <paramref name="filePath"/>, false otherwise</returns>
    bool TryGet(string filePath, out Guid analysisId, out IIssueConsumer issueConsumer);
    /// <summary>
    /// Discards the latest analysis for <paramref name="filePath"/>
    /// </summary>
    /// <param name="filePath">File system path for which the analysis was scheduled</param>
    void Remove(string filePath);
}
