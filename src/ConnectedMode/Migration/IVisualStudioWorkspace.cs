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

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// We can't mock of subclass VisualStudioWorkspace because it has a private constructor.
    /// The wrapper allows us to test the methods we use.
    /// </summary>
    internal interface IVisualStudioWorkspace
    {
        Solution CurrentSolution { get; }
        IVsHierarchy GetHierarchy(ProjectId projectId);
    }

    [ExcludeFromCodeCoverage]
    internal class VisualStudioWorkspaceWrapper : IVisualStudioWorkspace
    {
        private readonly VisualStudioWorkspace wrapped;

        public VisualStudioWorkspaceWrapper(VisualStudioWorkspace visualStudioWorkspace)
            => wrapped = visualStudioWorkspace;

        public Solution CurrentSolution => wrapped.CurrentSolution;

        public IVsHierarchy GetHierarchy(ProjectId projectId) => wrapped.GetHierarchy(projectId);
    }
}
