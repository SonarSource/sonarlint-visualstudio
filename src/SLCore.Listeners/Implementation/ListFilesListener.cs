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
using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation
{
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ListFilesListener : IListFilesListener
    {
        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly ISolutionWorkspaceService solutionWorkspaceService;
        private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
        private readonly IClientFileDtoFactory clientFileDtoFactory;

        [ImportingConstructor]
        public ListFilesListener(IFolderWorkspaceService folderWorkspaceService, ISolutionWorkspaceService solutionWorkspaceService,
            IActiveConfigScopeTracker activeConfigScopeTracker, IClientFileDtoFactory clientFileDtoFactory)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.solutionWorkspaceService = solutionWorkspaceService;
            this.activeConfigScopeTracker = activeConfigScopeTracker;
            this.clientFileDtoFactory = clientFileDtoFactory;
        }

        public Task<ListFilesResponse> ListFilesAsync(ListFilesParams parameters)
        {
            var clientFileDtos = new List<ClientFileDto>();
            if (activeConfigScopeTracker.Current.Id == parameters.configScopeId)
            {
                var fullFilePathList = folderWorkspaceService.IsFolderWorkspace() ? folderWorkspaceService.ListFiles() : solutionWorkspaceService.ListFiles();
                if (fullFilePathList.Any())
                {
                    var root = GetRoot(fullFilePathList.First());
                    if (activeConfigScopeTracker.TryUpdateRootOnCurrentConfigScope(parameters.configScopeId, root))
                    {
                        AddWorkspaceFilesDtos(parameters, clientFileDtos, root, fullFilePathList);
                    }
                }
            }
            return Task.FromResult(new ListFilesResponse(clientFileDtos));
        }

        private void AddWorkspaceFilesDtos(
            ListFilesParams parameters,
            List<ClientFileDto> clientFileDtos,
            string root,
            IReadOnlyCollection<string> fullFilePathList) =>
            clientFileDtos.AddRange(
                fullFilePathList
                    // bug
                    .Select(fp => clientFileDtoFactory.CreateOrNull(parameters.configScopeId, root, new SourceFile(fp)))
                    .Where(x => x is not null));

        private string GetRoot(string filePath)
        {
            var root = folderWorkspaceService.FindRootDirectory();

            root ??= Path.GetPathRoot(filePath);

            Debug.Assert(root != string.Empty);

            if (root[root.Length - 1] != Path.DirectorySeparatorChar)
            {
                root += Path.DirectorySeparatorChar;
            }

            return root;
        }
    }
}
