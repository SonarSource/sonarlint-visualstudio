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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    [Export(typeof(ISolutionInfoProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SolutionInfoProvider : ISolutionInfoProvider
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public SolutionInfoProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IThreadHandling threadHandling)
        {
            this.serviceProvider = serviceProvider;
            this.threadHandling = threadHandling;
        }

        public async Task<string> GetSolutionDirectoryAsync()
        {
            var slnFilePath = await GetFullSolutionFilePathAsync();
            if (slnFilePath == null) { return null; }

            return Path.GetDirectoryName(slnFilePath);
        }

        public string GetSolutionDirectory()
        {
            var slnFilePath = GetFullSolutionFilePath();
            if (slnFilePath == null) { return null; }

            return Path.GetDirectoryName(slnFilePath);
        }

        public async Task<string> GetFullSolutionFilePathAsync()
        {
            string fullSolutionName = null;
            await threadHandling.RunOnUIThreadAsync(() => fullSolutionName = GetSolutionFilePath());
            return fullSolutionName;
        }

        public string GetFullSolutionFilePath()
        {
            string fullSolutionName = null;
            threadHandling.RunOnUIThread(() => fullSolutionName = GetSolutionFilePath());
            return fullSolutionName;
        }

        public async Task<bool> IsSolutionFullyOpenedAsync()
        {
            var isOpen = false;
            await threadHandling.RunOnUIThreadAsync(() => isOpen = GetSolutionIsFullyOpened());
            return isOpen;
        }

        public bool IsSolutionFullyOpened()
        {
            var isOpen = false;
            threadHandling.RunOnUIThread(() => isOpen = GetSolutionIsFullyOpened());
            return isOpen;
        }

        public async Task<bool> IsFolderWorkspaceAsync()
        {
            var isFolderWorkspace = false;
            await threadHandling.RunOnUIThreadAsync(() => isFolderWorkspace = GetIsFolderWorkspace());
            return isFolderWorkspace;
        }

        public bool IsFolderWorkspace()
        {
            var isFolderWorkspace = false;
            threadHandling.RunOnUIThread(() => isFolderWorkspace = GetIsFolderWorkspace());
            return isFolderWorkspace;
        }

        private bool GetIsFolderWorkspace()
        {
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            // See https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.__vspropid7?view=visualstudiosdk-2019
            // The enum is not available in VS 2015 API.
            var VSPROPID_IsInOpenFolderMode = -8044;

            // "Open as Folder" was introduced in VS2017, so in VS2015 the hr result will be `E_NOTIMPL` and `isOpenAsFolder` will be null.
            var hr = solution.GetProperty(VSPROPID_IsInOpenFolderMode, out var isOpenAsFolder);
            Debug.Assert(hr == VSConstants.S_OK, "Failed to retrieve VSPROPID_IsInOpenFolderMode");

            return isOpenAsFolder != null && (bool)isOpenAsFolder;
        }

        private bool GetSolutionIsFullyOpened()
        {
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            object isLoaded;
            var hresult = solution.GetProperty((int)__VSPROPID4.VSPROPID_IsSolutionFullyLoaded, out isLoaded);

            return Microsoft.VisualStudio.ErrorHandler.Succeeded(hresult) && (isLoaded as bool?) == true;
        }

        private string GetSolutionFilePath()
        {
            // If there isn't an open solution the returned hresult will indicate an error
            // and the returned solution name will be null. We'll just ignore the hresult.
            IVsSolution solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out var fullSolutionName);
            return fullSolutionName as string;
        }
    }
}
