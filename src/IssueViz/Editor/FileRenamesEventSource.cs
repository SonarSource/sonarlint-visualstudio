/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    internal interface IFileRenamesEventSource : IDisposable
    {
        event EventHandler<FilesRenamedEventArgs> FilesRenamed;
    }

    internal class FilesRenamedEventArgs
    {
        public IReadOnlyDictionary<string, string> OldNewFilePaths { get; }

        public FilesRenamedEventArgs(IReadOnlyDictionary<string, string> oldNewFilePaths)
        {
            OldNewFilePaths = oldNewFilePaths;
        }
    }

    [Export(typeof(IFileRenamesEventSource))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class FileRenamesEventSource : IFileRenamesEventSource, IVsTrackProjectDocumentsEvents2
    {
        public event EventHandler<FilesRenamedEventArgs> FilesRenamed;

        private readonly IVsTrackProjectDocuments2 trackProjectDocuments;
        private readonly uint trackDocumentEventsCookie;

        [ImportingConstructor]
        public FileRenamesEventSource([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            trackProjectDocuments = (IVsTrackProjectDocuments2)serviceProvider.GetService(typeof(SVsTrackProjectDocuments));
            trackProjectDocuments.AdviseTrackProjectDocumentsEvents(this, out trackDocumentEventsCookie);
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
        {
            var affectedFiles = new Dictionary<string, string>();

            for (var i = 0; i < rgszMkOldNames.Length; i++)
            {
                var oldFileFullPath = rgszMkOldNames[i];
                var newFileFullPath = rgszMkNewNames[i];

                affectedFiles.Add(oldFileFullPath, newFileFullPath);
            }

            if (affectedFiles.Any())
            {
                FilesRenamed?.Invoke(this, new FilesRenamedEventArgs(affectedFiles));
            }

            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            trackProjectDocuments.UnadviseTrackProjectDocumentsEvents(trackDocumentEventsCookie);
        }

        #region IVsTrackProjectDocumentsEvents2

        int IVsTrackProjectDocumentsEvents2.OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags,
            VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames,
            VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames,
            VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
            VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYADDDIRECTORYRESULTS[] rgResults)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags,
            VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
            VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
            => VSConstants.S_OK;

        int IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, uint[] rgdwSccStatus)
            => VSConstants.S_OK;

        #endregion
    }
}
