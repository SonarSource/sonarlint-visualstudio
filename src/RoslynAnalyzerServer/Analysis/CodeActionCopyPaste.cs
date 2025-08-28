// /*
//  * SonarLint for Visual Studio
//  * Copyright (C) 2016-2025 SonarSource SA
//  * mailto:info AT sonarsource DOT com
//  *
//  * This program is free software; you can redistribute it and/or
//  * modify it under the terms of the GNU Lesser General Public
//  * License as published by the Free Software Foundation; either
//  * version 3 of the License, or (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  * Lesser General Public License for more details.
//  *
//  * You should have received a copy of the GNU Lesser General Public License
//  * along with this program; if not, write to the Free Software Foundation,
//  * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//  */

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

public class CodeActionCopyPaste
{
    internal static async IAsyncEnumerable<(Document oldDoc, Document newDoc)> GetChangedDocuments(
        Workspace workspace,
        Solution originalSolution,
        Solution changedSolution,
        CancellationToken cancellationToken)
    {
        var currentSolution = workspace.CurrentSolution;
        //
        // // if there was no intermediary edit, just apply the change fully.
        // if (workspace.TryApplyChanges(changedSolution))
        // {
        //     return true;
        // }

        // Otherwise, we need to see what changes were actually made and see if we can apply them.  The general rules are:
        //
        // 1. we only support text changes when doing merges.  Any other changes to projects/documents are not
        //    supported because it's very unclear what impact they may have wrt other workspace updates that have
        //    already happened.
        //
        // 2. For text changes, we only support it if the current text of the document we're changing itself has not
        //    changed. This means we can merge in edits if there were changes to unrelated files, but not if there
        //    are changes to the current file.  We could consider relaxing this in the future, esp. if we make use
        //    of some sort of text-merging-library to handle this.  However, the user would then have to handle diff
        //    markers being inserted into their code that they then have to handle.

        var solutionChanges = changedSolution.GetChanges(originalSolution);

        if (solutionChanges.GetAddedProjects().Any() ||
            solutionChanges.GetAddedAnalyzerReferences().Any() ||
            solutionChanges.GetRemovedProjects().Any() ||
            solutionChanges.GetRemovedAnalyzerReferences().Any())
        {
            yield break;
        }

        // Take the actual current solution the workspace is pointing to and fork it with just the text changes the
        // code action wanted to make.  Then apply that fork back into the workspace.
        var forkedSolution = currentSolution;

        foreach (var changedProject in solutionChanges.GetProjectChanges())
        {
            // We only support text changes.  If we see any other changes to this project, bail out immediately.
            if (changedProject.GetAddedAdditionalDocuments().Any() ||
                changedProject.GetAddedAnalyzerConfigDocuments().Any() ||
                changedProject.GetAddedAnalyzerReferences().Any() ||
                changedProject.GetAddedDocuments().Any() ||
                changedProject.GetAddedMetadataReferences().Any() ||
                changedProject.GetAddedProjectReferences().Any() ||
                changedProject.GetRemovedAdditionalDocuments().Any() ||
                changedProject.GetRemovedAnalyzerConfigDocuments().Any() ||
                changedProject.GetRemovedAnalyzerReferences().Any() ||
                changedProject.GetRemovedDocuments().Any() ||
                changedProject.GetRemovedMetadataReferences().Any() ||
                changedProject.GetRemovedProjectReferences().Any())
            {
                yield break;
            }

            // We have to at least have some changed document
            var changedDocuments = changedProject.GetChangedDocuments()
                .Concat(changedProject.GetChangedAdditionalDocuments())
                .Concat(changedProject.GetChangedAnalyzerConfigDocuments()).ToImmutableArray();

            if (changedDocuments.Length == 0)
            {
                yield break;
            }

            foreach (var documentId in changedDocuments)
            {
                var originalDocument = changedProject.OldProject.Solution.GetDocument(documentId);
                var changedDocument = changedProject.NewProject.Solution.GetDocument(documentId);

                // it has to be a text change the operation wants to make.  If the operation is making some other
                // sort of change, we can't merge this operation in.
                if (await changedDocument.GetTextVersionAsync(cancellationToken) == await originalDocument.GetTextVersionAsync(cancellationToken))
                {
                    yield break;
                }

                // If the document has gone away, we definitely cannot apply a text change to it.
                var currentDocument = currentSolution.GetDocument(documentId);
                if (currentDocument is null)
                {
                    yield break;
                }

                // If the file contents changed in the current workspace, then we can't apply this change to it.
                // Note: we could potentially try to do a 3-way merge in the future, including handling conflicts
                // with that.  For now though, we'll leave that out of scope.
                if (await originalDocument.GetTextVersionAsync(cancellationToken) != await currentDocument.GetTextVersionAsync(cancellationToken))
                {
                    yield break;
                }

                yield return (originalDocument, changedDocument);

                // forkedSolution = forkedSolution.WithDocumentText(documentId, await changedDocument.GetTextAsync(cancellationToken));
            }
        }
    }
}
