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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    internal interface IRoslynProjectProvider
    {
        /// <summary>
        /// Returns the list of Roslyn (C# and VB.NET) projects, if any
        /// </summary>
        IReadOnlyList<Project> Get();
    }

    [Export(typeof(IRoslynProjectProvider))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class RoslynProjectProvider : IRoslynProjectProvider
    {
        private readonly IVisualStudioWorkspace wrappedWorkspace;

        [ImportingConstructor]
        public RoslynProjectProvider(VisualStudioWorkspace vsWorkspace)
            : this(new VisualStudioWorkspaceWrapper(vsWorkspace))
        {
        }

        internal /* for testing */ RoslynProjectProvider(IVisualStudioWorkspace wrappedWorkspace)
        {
            this.wrappedWorkspace = wrappedWorkspace;
        }

        public IReadOnlyList<Project> Get()
        {
            if (wrappedWorkspace?.CurrentSolution == null)
            {
                return new List<Project>();
            }

            return wrappedWorkspace?.CurrentSolution?.Projects.ToList();
        }
    }
}
