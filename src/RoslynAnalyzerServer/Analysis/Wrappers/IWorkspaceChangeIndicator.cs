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

using Microsoft.CodeAnalysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

/// <summary>
/// Indicates whether workspace changes are critical and require reanalysis
/// </summary>
internal interface IWorkspaceChangeIndicator
{
    /// <summary>
    /// Checks if solution change event is not critical. Does not guarantee that it is critical if returns false.
    /// </summary>
    bool IsChangeKindTrivial(WorkspaceChangeKind kind);

    /// <summary>
    /// Checks if solution changes are critical and require reanalysis
    /// </summary>
    bool SolutionChangedCritically(SolutionChanges solutionChanges);

    /// <summary>
    /// Checks if project changes are critical and require reanalysis
    /// </summary>
    bool ProjectChangedCritically(ProjectChanges changedProject);
}
