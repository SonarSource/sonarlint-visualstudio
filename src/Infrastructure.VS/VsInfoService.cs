/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    /// <summary>
    /// Returns information about the current VS installation
    /// </summary>
    public interface IVsInfoService
    {
        /// <summary>
        /// Returns the root directory for executing VS installation
        /// </summary>
        string InstallRootDir { get; }
    }

    [Export(typeof(IVsInfoService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VsInfoService : IVsInfoService
    {
        [ImportingConstructor]
        public VsInfoService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            InstallRootDir = GetInstallRootDir(serviceProvider);
        }

        public string InstallRootDir { get; }

        private string GetInstallRootDir(IServiceProvider serviceProvider)
        {
            IVsShell shell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;

            object value;
            ErrorHandler.ThrowOnFailure(shell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out value));
            return value as string;
        }
    }
}
