/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    [Export(typeof(IStatusRequestHandler))]
    internal class StatusRequestHandler : IStatusRequestHandler
    {
        private readonly IVsShell vsShell;
        private readonly IVsSolution vsSolution;

        [ImportingConstructor]
        public StatusRequestHandler([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        }

        Task<IStatusResponse> IStatusRequestHandler.GetStatusAsync()
        {
            vsShell.GetProperty((int) __VSSPROPID5.VSSPROPID_AppBrandName, out var ideName);
            ideName = ideName ?? "Microsoft Visual Studio";

            vsSolution.GetProperty((int) __VSPROPID.VSPROPID_SolutionBaseName, out var ideInstance);
            ideInstance = ideInstance ?? "";

            return Task.FromResult<IStatusResponse>(new StatusResponse(ideName.ToString(), ideInstance.ToString()));
        }
    }
}
