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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

[ExcludeFromCodeCoverage] // todo SLVS-2466 add roslyn 'integration' tests using AdHocWorkspace
[Export(typeof(IWorkspaceChangeIndicator))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class WorkspaceChangeIndicator : IWorkspaceChangeIndicator
{
    private readonly HashSet<WorkspaceChangeKind> trivialChanges =
    [
        // Currently changes in other files should not affect too many quickfixes
        // There is an opportunity for improvement:
        // this group needs to be removed, and a mechanism that either re-analyzes on apply fail,
        // or a background analysis job, need to be added
        // todo https://sonarsource.atlassian.net/browse/SLVS-2612
        WorkspaceChangeKind.DocumentAdded,
        WorkspaceChangeKind.DocumentRemoved,
        WorkspaceChangeKind.DocumentReloaded,
        WorkspaceChangeKind.DocumentChanged,
        WorkspaceChangeKind.DocumentInfoChanged,
        WorkspaceChangeKind.AdditionalDocumentAdded,
        WorkspaceChangeKind.AdditionalDocumentRemoved,
        WorkspaceChangeKind.AdditionalDocumentReloaded,
        WorkspaceChangeKind.AdditionalDocumentChanged,

        // we don't care about other analyzers
        WorkspaceChangeKind.AnalyzerConfigDocumentAdded,
        WorkspaceChangeKind.AnalyzerConfigDocumentRemoved,
        WorkspaceChangeKind.AnalyzerConfigDocumentReloaded,
        WorkspaceChangeKind.AnalyzerConfigDocumentChanged,
    ];

    public bool IsChangeKindTrivial(WorkspaceChangeKind kind) => trivialChanges.Contains(kind);

    public bool SolutionChangedCritically(SolutionChanges solutionChanges) =>
        solutionChanges.GetAddedProjects().Any() ||
        solutionChanges.GetRemovedProjects().Any();

        // We don't care about other analyzers. This is preserved for documentation's sake, as this code was originally copied from roslyn's github
        // solutionChanges.GetAddedAnalyzerReferences().Any() ||
        // solutionChanges.GetRemovedAnalyzerReferences().Any();

    public bool ProjectChangedCritically(ProjectChanges changedProject) =>
        changedProject.GetAddedMetadataReferences().Any() ||
        changedProject.GetAddedProjectReferences().Any() ||
        changedProject.GetRemovedMetadataReferences().Any() ||
        changedProject.GetRemovedProjectReferences().Any();

        // todo https://sonarsource.atlassian.net/browse/SLVS-2612
        // Currently changes in other files should not affect too many quickfixes, see comment above.
        // changedProject.GetAddedDocuments().Any() ||
        // changedProject.GetRemovedDocuments().Any() ||
        // changedProject.GetAddedAdditionalDocuments().Any() ||
        // changedProject.GetRemovedAdditionalDocuments().Any() ||

        // We don't care about configs as we get rules configuration from SLCore. This is preserved for documentation's sake, as this code was originally copied from roslyn's github
        // changedProject.GetAddedAnalyzerConfigDocuments().Any() ||
        // changedProject.GetRemovedAnalyzerConfigDocuments().Any() ||

        // We don't care about other analyzers or suppressors. This is preserved for documentation's sake, as this code was originally copied from roslyn's github
        // changedProject.GetAddedAnalyzerReferences().Any() ||
        // changedProject.GetRemovedAnalyzerReferences().Any() ||
}
