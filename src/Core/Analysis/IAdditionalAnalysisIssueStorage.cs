/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Collections.Concurrent;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core.Analysis;

public interface IAdditionalAnalysisIssueStorage
{
    void Store(string filePath, IReadOnlyList<IAnalysisIssue> issues);
    IReadOnlyList<IAnalysisIssue> Get(string filePath);
    void Remove(string filePath);
}

[Export(typeof(IAdditionalAnalysisIssueStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class AdditionalAnalysisIssueStorage : IAdditionalAnalysisIssueStorage
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<IAnalysisIssue>> storage = new();

    public void Store(string filePath, IReadOnlyList<IAnalysisIssue> issues) =>
        storage[filePath] = issues;

    public IReadOnlyList<IAnalysisIssue> Get(string filePath) =>
        storage.TryGetValue(filePath, out var issues) ? issues : [];

    public void Remove(string filePath) =>
        storage.TryRemove(filePath, out _);
}
