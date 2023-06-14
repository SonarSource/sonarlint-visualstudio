/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    internal interface IRoslynProjectWalker
    {
        /// <summary>
        /// Walks the list of Roslyn (C# and VB.NET) projects and
        /// return the corresponding IVsHiearchy object, if available
        /// </summary>
        IEnumerable<IVsHierarchy> Walk();
    }

    [Export(typeof(IRoslynProjectWalker))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class RoslynProjectWalker : IRoslynProjectWalker
    {
        private readonly IVisualStudioWorkspace wrappedWorkspace;
        private readonly ILogger logger;

        [ImportingConstructor]
        public RoslynProjectWalker(VisualStudioWorkspace vsWorkspace, ILogger logger)
        {
        }

        internal /* for testing */ RoslynProjectWalker(IVisualStudioWorkspace wrappedWorkspace, ILogger logger)
        {
            this.wrappedWorkspace = wrappedWorkspace;
            this.logger = logger;
        }

        public IEnumerable<IVsHierarchy> Walk()
        {
            LogVerbose($"[Migration] Fetching Roslyn projects... Count: {wrappedWorkspace.CurrentSolution?.Projects.Count() ?? 0}");

            if (wrappedWorkspace.CurrentSolution == null)
            {
                yield break;
            }

            foreach (var project in wrappedWorkspace.CurrentSolution?.Projects)
            {
                // Note: the RoslynVisualStudioWorkspace has an immutable dictionary of
                // project -> IVsHierarchy, so it doesn't need to be on the UI thread to
                // return the hierarchy. However, that is an internal implementation 
                // detail that could change in the future.
                LogVerbose($" Processing project: {project.Name}");
                var vsHierarchy = wrappedWorkspace.GetHierarchy(project.Id);

                if (vsHierarchy == null)
                {
                    LogVerbose($" Could not get IVsHierarchy for project: {project.Name}");
                }
                else
                {
                    yield return vsHierarchy;
                }
            }
        }

        private void LogVerbose(string message)
            => logger.LogVerbose("[Migration] " + message);
    }
}
