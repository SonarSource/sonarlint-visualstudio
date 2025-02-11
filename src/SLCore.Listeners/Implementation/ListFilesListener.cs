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
        private readonly ILogger logger;

        [ImportingConstructor]
        public ListFilesListener(
            IFolderWorkspaceService folderWorkspaceService,
            ISolutionWorkspaceService solutionWorkspaceService,
            IActiveConfigScopeTracker activeConfigScopeTracker,
            IClientFileDtoFactory clientFileDtoFactory,
            ILogger logger)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.solutionWorkspaceService = solutionWorkspaceService;
            this.activeConfigScopeTracker = activeConfigScopeTracker;
            this.clientFileDtoFactory = clientFileDtoFactory;
            this.logger = logger.ForContext("Open In IDE").ForContext(nameof(ListFilesListener));
        }

        public Task<ListFilesResponse> ListFilesAsync(ListFilesParams parameters)
        {
            var clientFileDtos = new List<ClientFileDto>();
            if (activeConfigScopeTracker.Current.Id == parameters.configScopeId)
            {
                var isFolderWorkspace = folderWorkspaceService.IsFolderWorkspace();
                logger.LogVerbose($"Is folder workspace: {isFolderWorkspace}");
                var fullFilePathList = isFolderWorkspace ? folderWorkspaceService.ListFiles() : solutionWorkspaceService.ListFiles();
                if (fullFilePathList.Any())
                {
                    var root = GetRoot(fullFilePathList.First());
                    logger.LogVerbose($"Root {root}");
                    if (activeConfigScopeTracker.TryUpdateRootOnCurrentConfigScope(parameters.configScopeId, root))
                    {
                        clientFileDtos.AddRange(fullFilePathList.Select(fp => clientFileDtoFactory.Create(parameters.configScopeId, root, new SourceFile(fp))));
                    }
                }

                logger.LogVerbose($"Files: {string.Join(Environment.NewLine, clientFileDtos.Select(x => $"{x.ideRelativePath} -> {x.fsPath}"))}");
            }
            return Task.FromResult(new ListFilesResponse(clientFileDtos));
        }

        private string GetRoot(string filePath)
        {
            logger.LogVerbose($"File to calculate root {filePath}");
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
