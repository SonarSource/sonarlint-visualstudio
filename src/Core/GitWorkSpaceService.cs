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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Core;

public interface IGitWorkspaceService
{
    /// <summary>
    /// Returns the file path to the current Git repository root
    /// Or null if not a git repo
    /// </summary>
    string GetRepoRoot();
}

[Export(typeof(IGitWorkspaceService))]
internal class GitWorkspaceService : IGitWorkspaceService
{
    private readonly ILogger logger;
    private readonly ISolutionInfoProvider solutionInfoProvider;
    private readonly IFileSystem fileSystem;

    private const string GitFolder = ".git";

    private record struct GitWorkspaceCache(string WorkspaceRoot, string GitRoot);
    private readonly object lockObject = new();
    private GitWorkspaceCache currentWorkspace;


    [ImportingConstructor]
    public GitWorkspaceService(ISolutionInfoProvider solutionInfoProvider, ILogger logger, IFileSystemService fileSystem)
    {
        this.solutionInfoProvider = solutionInfoProvider;
        this.logger = logger;
        this.fileSystem = fileSystem;
    }

    public string GetRepoRoot()
    {
        var workspaceRoot = solutionInfoProvider.GetSolutionDirectory();

        lock (lockObject)
        {
            if (currentWorkspace.WorkspaceRoot == workspaceRoot)
            {
                return currentWorkspace.GitRoot;
            }

            currentWorkspace = new GitWorkspaceCache(workspaceRoot, Calculate(workspaceRoot));
            return currentWorkspace.GitRoot;
        }
    }

    private string Calculate(string workspaceRoot)
    {
        if (workspaceRoot == null)
        {
            return null;
        }

        var currentDir = new DirectoryInfo(workspaceRoot);

        do
        {
            var gitRoot = Path.Combine(currentDir.FullName, GitFolder);

            if (fileSystem.Directory.Exists(gitRoot))
            {
                return currentDir.FullName;
            }

            currentDir = currentDir.Parent;
        } while (currentDir != null && fileSystem.Directory.Exists(currentDir.FullName));

        logger.WriteLine(CoreStrings.NoGitFolder);
        return null;
    }
}
