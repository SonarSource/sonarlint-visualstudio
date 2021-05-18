/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    /// <summary>
    /// MEF-exportable wrapper for <see cref="IProjectSystemHelper.GetFileVsHierarchy"/>
    /// </summary>
    [Export(typeof(IVsHierarchyLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VsHierarchyLocator : IVsHierarchyLocator
    {
        private readonly IProjectSystemHelper projectSystemHelper;

        [ImportingConstructor]
        public VsHierarchyLocator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : this(new ProjectSystemHelper(serviceProvider))
        {
        }

        internal VsHierarchyLocator(IProjectSystemHelper projectSystemHelper)
        {
            this.projectSystemHelper = projectSystemHelper;
        }

        public IVsHierarchy GetFileVsHierarchy(string fileName) => projectSystemHelper.GetFileVsHierarchy(fileName);
    }
}
