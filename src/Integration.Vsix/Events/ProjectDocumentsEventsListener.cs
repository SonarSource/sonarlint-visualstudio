﻿/*
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.Events;

[Export(typeof(IProjectDocumentsEventsListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class ProjectDocumentsEventsListener : IProjectDocumentsEventsListener, IVsTrackProjectDocumentsEvents2
{
    private readonly IFileTracker fileTracker;
    private readonly IThreadHandling threadHandling;
    private readonly IVsUIServiceOperation serviceOperation;
    private uint trackDocumentEventsCookie;
    private bool isDisposed;

    [ImportingConstructor]
    public ProjectDocumentsEventsListener(IFileTracker fileTracker, IThreadHandling threadHandling, IVsUIServiceOperation serviceOperation)
    {
        this.fileTracker = fileTracker;
        this.threadHandling = threadHandling;
        this.serviceOperation = serviceOperation;
    }

    public void Initialize()
    {
        serviceOperation.Execute<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(trackProjectDocuments =>
        {
            threadHandling.ThrowIfNotOnUIThread();
            Debug.Assert(trackProjectDocuments != null, "Cannot find IVsTrackProjectDocuments2");
            ErrorHandler.ThrowOnFailure(trackProjectDocuments.AdviseTrackProjectDocumentsEvents(this, out trackDocumentEventsCookie));
        });
    }

    public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags,
        VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
    {
        return VSConstants.S_OK;
    }

    public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
        string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
    {
        threadHandling.ThrowIfNotOnUIThread();
        fileTracker.AddFiles(rgpszMkDocuments.Select(fp => new SourceFile(fp)).ToArray());
        return VSConstants.S_OK;
    }

    public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
        string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
    {
        return VSConstants.S_OK;
    }

    public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
        string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
    {
        threadHandling.ThrowIfNotOnUIThread();
        fileTracker.RemoveFiles(rgpszMkDocuments);
        return VSConstants.S_OK;
    }

    public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
        string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
    {
        return VSConstants.S_OK;
    }

    public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames,
        VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
    {
        return VSConstants.S_OK;
    }

    public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
        string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
    {
        threadHandling.ThrowIfNotOnUIThread();
        fileTracker.RenameFiles(rgszMkOldNames, rgszMkNewNames.Select(fp => new SourceFile(fp)).ToArray());
        return VSConstants.S_OK;
    }

    public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames,
        VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult,
        VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
    {
        return VSConstants.S_OK;
    }

    public int OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices,
        string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
    {
        return VSConstants.S_OK;
    }

    public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
        VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult,
        VSQUERYADDDIRECTORYRESULTS[] rgResults)
    {
        return VSConstants.S_OK;
    }

    public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags,
        VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
    {
        return VSConstants.S_OK;
    }

    public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
        VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult,
        VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
    {
        return VSConstants.S_OK;
    }

    public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
        string[] rgpszMkDocuments, uint[] rgdwSccStatus)
    {
        return VSConstants.S_OK;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        serviceOperation.Execute<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(trackProjectDocuments =>
        {
            threadHandling.ThrowIfNotOnUIThread();
            Debug.Assert(trackProjectDocuments != null, "Cannot find IVsTrackProjectDocuments2");
            ErrorHandler.ThrowOnFailure(trackProjectDocuments.UnadviseTrackProjectDocumentsEvents(trackDocumentEventsCookie));
        });

        isDisposed = true;
    }
}
