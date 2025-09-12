// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using Document = Microsoft.CodeAnalysis.Document;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer;

// This class was adapted from https://github.com/dotnet/roslyn/blob/75e79dace86b274327a1afe479228d82a06051a4/src/Workspaces/Core/Portable/CodeActions/Operations/ApplyChangesOperation.cs#L46
[ExcludeFromCodeCoverage]
public static class ApplyChangesOperation
{
    internal static async Task<bool> ApplyOrMergeChangesAsync(
        Workspace workspace,
        Solution originalSolution,
        Solution changedSolution,
        ILogger logger,
        IWorkspaceChangeIndicator workspaceChangeIndicator,
        CancellationToken cancellationToken)
    {
        var currentSolution = workspace.CurrentSolution;

        // if there was no intermediary edit, just apply the change fully.
        if (workspace.TryApplyChanges(changedSolution))
        {
            return true;
        }

        // Otherwise, we need to see what changes were actually made and see if we can apply them.  The general rules are:
        //
        // 1. we only support text changes when doing merges.  Any other changes to projects/documents are not
        //    supported because it's very unclear what impact they may have wrt other workspace updates that have
        //    already happened.
        //
        // 2. For text changes, we only support it if the current text of the document we're changing itself has not
        //    changed. This means we can merge in edits if there were changes to unrelated files, but not if there
        //    are changes to the current file.

        var solutionChanges = changedSolution.GetChanges(originalSolution);

        if (workspaceChangeIndicator.SolutionChangedCritically(solutionChanges))
        {
            logger.LogVerbose(Resources.ApplyChangesOperation_SolutionChanged);
            return false;
            // todo https://sonarsource.atlassian.net/browse/SLVS-2513 this will lead to invalid quickfixes if project configuration changes.
            // do we need to reanalyze open files on major workspace changes? can modified analyzer references be ignored?
        }

        // Take the actual current solution the workspace is pointing to and fork it with just the text changes the
        // code action wanted to make.  Then apply that fork back into the workspace.
        var forkedSolution = currentSolution;

        foreach (var changedProject in solutionChanges.GetProjectChanges())
        {
            // We only support text changes.  If we see any other changes to this project, bail out immediately.
            if (workspaceChangeIndicator.ProjectChangedCritically(changedProject))
            {
                logger.LogVerbose(Resources.ApplyChangesOperation_ProjectChanged, changedProject.NewProject.Name);
                return false;
            }

            // We have to at least have some changed document
            var changedDocuments = GetChangedDocuments(changedProject);

            if (changedDocuments.Length == 0)
            {
                return false;
            }

            foreach (var documentId in changedDocuments)
            {
                if (!GetDocuments(changedProject, documentId, out var originalDocument, out var changedDocument))
                {
                    return false;
                }

                // it has to be a text change the operation wants to make.  If the operation is making some other
                // sort of change, we can't merge this operation in.
                if (await changedDocument.GetTextVersionAsync(cancellationToken) == await originalDocument.GetTextVersionAsync(cancellationToken))
                {
                    return false;
                }

                // If the document has gone away, we definitely cannot apply a text change to it.
                var currentDocument = currentSolution.GetDocument(documentId);
                if (currentDocument is null)
                {
                    return false;
                }

                // If the file contents changed in the current workspace, then we can't apply this change to it.
                if (await originalDocument.GetTextVersionAsync(cancellationToken) != await currentDocument.GetTextVersionAsync(cancellationToken))
                {
                    return false;
                }

                forkedSolution = forkedSolution.WithDocumentText(documentId, await changedDocument.GetTextAsync(cancellationToken));
            }
        }

        return workspace.TryApplyChanges(forkedSolution);
    }

    private static ImmutableArray<DocumentId> GetChangedDocuments(ProjectChanges changedProject)
    {
        var changedDocuments = changedProject.GetChangedDocuments()
            .Concat(changedProject.GetChangedAdditionalDocuments())
            .Concat(changedProject.GetChangedAnalyzerConfigDocuments()).ToImmutableArray();
        return changedDocuments;
    }

    private static bool GetDocuments(
        ProjectChanges changedProject,
        DocumentId documentId,
        [NotNullWhen(true)]out Document? originalDocument,
        [NotNullWhen(true)]out Document? changedDocument)
    {
        originalDocument = changedProject.OldProject.Solution.GetDocument(documentId);

        if (originalDocument == null)
        {
            Debug.Fail("Original document not found");
            changedDocument = null;
            return false;
        }

        changedDocument = changedProject.NewProject.Solution.GetDocument(documentId);

        if (changedDocument == null)
        {
            Debug.Fail("Changed document not found");
            return false;
        }
        return true;
    }
}
