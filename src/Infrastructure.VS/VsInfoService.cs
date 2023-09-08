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

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
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
        private readonly Lazy<string> installRootDir;

        [ImportingConstructor]
        public VsInfoService(IVsUIServiceOperation vsUIServiceOperation)
        {
            installRootDir = new Lazy<string>(() => vsUIServiceOperation.Execute<SVsShell, IVsShell, string>(GetInstallRootDir));
        }

        public string InstallRootDir => installRootDir.Value;

        private string GetInstallRootDir(IVsShell shell)
        {
            object value;
            ErrorHandler.ThrowOnFailure(shell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out value));
            return value as string;
        }
    }
}
