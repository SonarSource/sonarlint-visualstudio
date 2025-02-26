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
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class ListFilesListener(
    IFolderWorkspaceService folderWorkspaceService,
    ISolutionWorkspaceService solutionWorkspaceService,
    ISharedBindingConfigProvider sharedBindingConfigProvider,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IClientFileDtoFactory clientFileDtoFactory)
    : IListFilesListener
{
    private readonly List<ClientFileDto> NoFiles = [];

    public Task<ListFilesResponse> ListFilesAsync(ListFilesParams parameters) => Task.FromResult(new ListFilesResponse(GetFilesList(parameters)));

    private List<ClientFileDto> GetFilesList(ListFilesParams parameters)
    {
        if (activeConfigScopeTracker.Current.Id != parameters.configScopeId)
        {
            return NoFiles;
        }

        if (GetWorkspaceFiles() is not { Count: > 0 } workspaceFilePaths)
        {
            return NoFiles;
        }

        if (GetRoot(workspaceFilePaths.First()) is not { } root
            || !activeConfigScopeTracker.TryUpdateRootOnCurrentConfigScope(parameters.configScopeId, root))
        {
            return NoFiles;
        }

        return GetClientFilesDtos(parameters, root, AddExtraFiles(workspaceFilePaths));
    }

    private IEnumerable<string> AddExtraFiles(IReadOnlyCollection<string> workspaceFilePaths) =>
        sharedBindingConfigProvider.GetSharedBindingFilePathOrNull() is { } sharedBindingFilePath
            ? workspaceFilePaths.Append(sharedBindingFilePath)
            : workspaceFilePaths;

    private IReadOnlyCollection<string> GetWorkspaceFiles() =>
        folderWorkspaceService.IsFolderWorkspace()
            ? folderWorkspaceService.ListFiles()
            : solutionWorkspaceService.ListFiles();

    private List<ClientFileDto> GetClientFilesDtos(
        ListFilesParams parameters,
        string root,
        IEnumerable<string> fullFilePathList) =>
        fullFilePathList
            .Select(fp => clientFileDtoFactory.CreateOrNull(parameters.configScopeId, root, new SourceFile(fp)))
            .Where(x => x is not null)
            .ToList();

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
