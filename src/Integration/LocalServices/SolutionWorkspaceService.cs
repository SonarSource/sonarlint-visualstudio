/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(ISolutionWorkspaceService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SolutionWorkspaceService : ISolutionWorkspaceService
    {
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public SolutionWorkspaceService(ISolutionInfoProvider solutionInfoProvider, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.solutionInfoProvider = solutionInfoProvider;
            this.serviceProvider = serviceProvider;
        }

        public bool IsSolutionWorkSpace() => !solutionInfoProvider.IsFolderWorkspace();

        [ExcludeFromCodeCoverage]
        public IEnumerable<string> ListFiles()
        {
            if (!IsSolutionWorkSpace()) { return Array.Empty<string>(); }

            IVsSolution solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();

            return GetAllFilesInSolution(solution);
        }

        private IEnumerable<string> GetAllFilesInSolution(IVsSolution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return GetLoadedProjects(solution)
                .SelectMany(AllItemsInProject)
                .Where(x => x != null)
                .Where(x => x.Contains("\\"))
                .Where(x => !x.EndsWith("\\"))
                .Where(x => !x.Contains("\\.nuget\\"))
                .Where(x => !x.Contains("\\node_modules\\")) // move filtering closer to path extraction to avoid processing unnecessary items
                .Distinct(); // might not be needed
        }

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

        private IEnumerable<string> AllItemsInProject(IVsProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            var projectDir = Path.GetDirectoryName(GetProjectFilePath(project));
            var hierarchy = project as IVsHierarchy;

            return
                ChildrenOf(hierarchy, VSConstants.VSITEMID.Root)
                    .Select(
                        id =>
                        {
                            project.GetMkDocument((uint)id, out var name);
                            if (name != null && projectDir != null && !name.StartsWith(projectDir))
                            {// sometimes random sdk files are included as parts of project items
                                return null;
                            }
                            if (name != null && name.Length > 0 && !Path.IsPathRooted(name))
                            { // might not be needed at all
                                name = AbsolutePathFromRelative(name, projectDir);
                            }
                            return name;
                        });
            // .Where(File.Exists); // too slow, no false positives
        }

        private string GetProjectFilePath(IVsProject project)
        {
            var path = string.Empty;
            var hr = project.GetMkDocument((uint)VSConstants.VSITEMID.Root, out path);
            Debug.Assert(hr == VSConstants.S_OK || hr == VSConstants.E_NOTIMPL, "GetMkDocument failed for project.");

            return path;
        }

        private IEnumerable<VSConstants.VSITEMID> ChildrenOf(IVsHierarchy hierarchy, VSConstants.VSITEMID rootID)
        {
            var result = new List<VSConstants.VSITEMID>(); // this list is not needed

            for (var itemID = FirstChild(hierarchy, rootID);
                 itemID != VSConstants.VSITEMID.Nil;
                 itemID = NextSibling(hierarchy, itemID))
            {
                result.Add(itemID);
                result.AddRange(ChildrenOf(hierarchy, itemID)); // TODO get rid of recursion
            }

            return result;
        }

        private static VSConstants.VSITEMID FirstChild(IVsHierarchy hierarchy, VSConstants.VSITEMID rootID)
        {
            hierarchy.GetProperty((uint)rootID, (int)__VSHPROPID.VSHPROPID_FirstChild, out var childIDObj);
            if (childIDObj != null)
            {
                return (VSConstants.VSITEMID)(int)childIDObj;
            }

            return VSConstants.VSITEMID.Nil;
        }

        private static VSConstants.VSITEMID NextSibling(IVsHierarchy hierarchy, VSConstants.VSITEMID firstID)
        {
            hierarchy.GetProperty((uint)firstID, (int)__VSHPROPID.VSHPROPID_NextSibling, out var siblingIDObj);
            if (siblingIDObj != null)
            {
                return (VSConstants.VSITEMID)(int)siblingIDObj;
            }

            return VSConstants.VSITEMID.Nil;
        }

        /// <summary>
        /// Transforms a relative path to an absolute one based on a specified base folder.
        /// </summary>
        private string AbsolutePathFromRelative(string relativePath, string baseFolderForDerelativization)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException("relativePath");
            }
            if (baseFolderForDerelativization == null)
            {
                throw new ArgumentNullException("baseFolderForDerelativization");
            }
            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("", "relativePath");
            }
            if (!Path.IsPathRooted(baseFolderForDerelativization))
            {
                throw new ArgumentException("", "baseFolderForDerelativization");
            }
            StringBuilder result = new StringBuilder(baseFolderForDerelativization);
            if (result[result.Length - 1] != Path.DirectorySeparatorChar)
            {
                result.Append(Path.DirectorySeparatorChar);
            }
            int spanStart = 0;
            while (spanStart < relativePath.Length)
            {
                int spanStop = relativePath.IndexOf(Path.DirectorySeparatorChar, spanStart);
                if (spanStop == -1)
                {
                    spanStop = relativePath.Length;
                }
                string span = relativePath.Substring(spanStart, spanStop - spanStart);
                if (span == "..")
                {
                    // The result string should end with a directory separator at this point.  We
                    // want to search for the one previous to that, which is why we subtract 2.
                    int previousSeparator;
                    if (result.Length < 2 || (previousSeparator = result.ToString().LastIndexOf(Path.DirectorySeparatorChar, result.Length - 2)) == -1)
                    {
                        throw new ArgumentException("");
                    }
                    result.Remove(previousSeparator + 1, result.Length - previousSeparator - 1);
                }
                else if (span != ".")
                {
                    // Ignore "." because it means the current direcotry
                    result.Append(span);
                    if (spanStop < relativePath.Length)
                    {
                        result.Append(Path.DirectorySeparatorChar);
                    }
                }
                spanStart = spanStop + 1;
            }
            return result.ToString();
        }
    }
}
