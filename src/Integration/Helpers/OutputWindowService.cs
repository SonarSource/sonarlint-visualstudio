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
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IOutputWindowService))]
    internal class OutputWindowService : IOutputWindowService
    {
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public OutputWindowService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void Show()
        {
            var sonarLintOutputPane = VsShellUtils.GetOrCreateSonarLintOutputPane(serviceProvider);
            Debug.Assert(sonarLintOutputPane != null, "Failed to create SonarLint pane");

            if (sonarLintOutputPane == null)
            {
                return;
            }
       
            var hr = sonarLintOutputPane.Activate();
            Debug.Assert(ErrorHandler.Succeeded(hr), "Failed to activate SonarLint pane: " + hr);

            if (!ErrorHandler.Succeeded(hr))
            {
                return;
            }

            ShowOutputWindowFrame();
        }

        private void ShowOutputWindowFrame()
        {
            var shell = serviceProvider.GetService<SVsUIShell, IVsUIShell>();
            var hr = shell.FindToolWindow(0, VSConstants.StandardToolWindows.Output, out var windowFrame);
            Debug.Assert(ErrorHandler.Succeeded(hr), "Call to find output window: " + hr);

            if (ErrorHandler.Succeeded(hr))
            {
                windowFrame?.Show();
            }
        }
    }
}
