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
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration;

[Export(typeof(ISolutionWorkspaceService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class SolutionWorkspaceService(ISolutionInfoProvider solutionInfoProvider, ILogger log, IVsUIServiceOperation vsUiServiceOperation)
    : ISolutionWorkspaceService
{
    public bool IsSolutionWorkSpace() => !solutionInfoProvider.IsFolderWorkspace();

    [ExcludeFromCodeCoverage]
    public IReadOnlyCollection<string> ListFiles() =>
        IsSolutionWorkSpace()
            ? vsUiServiceOperation.Execute<SVsSolution, IVsSolution, IReadOnlyCollection<string>>(GetAllFilesInSolution)
            : [];

    [ExcludeFromCodeCoverage]
    private IReadOnlyCollection<string> GetAllFilesInSolution(IVsSolution solution) =>
        GetLoadedProjects(solution)
            .SelectMany(AllItemsInProject)
            .Where(x => x != null)
            .Where(x => x.Contains("\\"))
            .Where(x => !x.EndsWith("\\"))
            .Where(x => !x.Contains("\\.nuget\\"))
            .Where(x => !x.Contains("\\node_modules\\"))
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase); // move filtering closer to path extraction to avoid processing unnecessary items)

    [ExcludeFromCodeCoverage]
    private IEnumerable<IVsProject> GetLoadedProjects(IVsSolution solution)
    {
        var guid = Guid.Empty;
        solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out var enumerator);
        var hierarchy = new IVsHierarchy[1] { null };
        for (enumerator.Reset();
             enumerator.Next(1, hierarchy, out var fetched) == VSConstants.S_OK && fetched == 1; /*nothing*/)
        {
            yield return (IVsProject)hierarchy[0];
        }
    }

    [ExcludeFromCodeCoverage]
    private IEnumerable<string> AllItemsInProject(IVsProject project)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        var projectFilePath = GetProjectFilePath(project);
        var projectDir = Path.GetDirectoryName(projectFilePath);
        var hierarchy = project as IVsHierarchy;

        return
            ChildrenOf(hierarchy, VSConstants.VSITEMID.Root)
                .Select(id =>
                {
                    if (project.GetMkDocument((uint)id, out var name) == VSConstants.S_OK)
                    {
                        if (name is { Length: > 0 } && Path.IsPathRooted(name))
                        {
                            return name;
                        }
                        if (projectDir != null)
                        {
                            return Path.Combine(projectDir, name);
                        }

                        log.LogVerbose("Could not build path for {0} in {1}, ignoring", name, projectFilePath);
                    }
                    return null;
                });
    }

    [ExcludeFromCodeCoverage]
    private string GetProjectFilePath(IVsProject project)
    {
        var path = string.Empty;
        var hr = project.GetMkDocument((uint)VSConstants.VSITEMID.Root, out path);
        Debug.Assert(hr == VSConstants.S_OK || hr == VSConstants.E_NOTIMPL, "GetMkDocument failed for project.");

        return path;
    }

    [ExcludeFromCodeCoverage]
    private IEnumerable<VSConstants.VSITEMID> ChildrenOf(IVsHierarchy hierarchy, VSConstants.VSITEMID rootID)
    {
        if (hierarchy.GetProperty((uint)rootID, (int)__VSHPROPID.VSHPROPID_Caption, out var caption) == VSConstants.S_OK && (string)caption == "External Dependencies")
        {
            return [];
        }

        var result = new List<VSConstants.VSITEMID>();

        for (var itemID = FirstChild(hierarchy, rootID);
             itemID != VSConstants.VSITEMID.Nil;
             itemID = NextSibling(hierarchy, itemID))
        {
            result.Add(itemID);
            result.AddRange(ChildrenOf(hierarchy, itemID));
        }

        return result;
    }

    [ExcludeFromCodeCoverage]
    private VSConstants.VSITEMID FirstChild(IVsHierarchy hierarchy, VSConstants.VSITEMID rootID)
    {
        try
        {
            if (hierarchy.GetProperty((uint)rootID, (int)__VSHPROPID.VSHPROPID_FirstChild, out var childIDObj) == VSConstants.S_OK && childIDObj != null)
            {
                return (VSConstants.VSITEMID)(int)childIDObj;
            }
        }
        catch (Exception e)
        {
            log.LogVerbose(e.ToString());
        }

        return VSConstants.VSITEMID.Nil;
    }

    [ExcludeFromCodeCoverage]
    private VSConstants.VSITEMID NextSibling(IVsHierarchy hierarchy, VSConstants.VSITEMID firstID)
    {
        try
        {
            if (hierarchy.GetProperty((uint)firstID, (int)__VSHPROPID.VSHPROPID_NextSibling, out var siblingIDObj) == VSConstants.S_OK && siblingIDObj != null)
            {
                return (VSConstants.VSITEMID)(int)siblingIDObj;
            }
        }
        catch (Exception e)
        {
            log.LogVerbose(e.ToString());
        }

        return VSConstants.VSITEMID.Nil;
    }
}
