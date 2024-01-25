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
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IToolWindowService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ToolWindowService : IToolWindowService
    {
        private readonly IVsUIServiceOperation vsUIServiceOperation;

        [ImportingConstructor]
        public ToolWindowService(IVsUIServiceOperation vsUIServiceOperation)
        {
            this.vsUIServiceOperation = vsUIServiceOperation;
        }

        public void Show(Guid toolWindowId)
        {
            vsUIServiceOperation.Execute<SVsUIShell, IVsUIShell>(
                shell =>
                {
                    var frame = GetOrCreateWindowFrame(shell, toolWindowId);
                    frame?.Show();
                });
        }

        public void EnsureToolWindowExists(Guid toolWindowId)
        {
            vsUIServiceOperation.Execute<SVsUIShell, IVsUIShell>(
                shell =>
                {
                    GetOrCreateWindowFrame(shell, toolWindowId);
                });
        }

        public V GetToolWindow<T, V>() where T : class
        {
            var docView = vsUIServiceOperation.Execute<SVsUIShell, IVsUIShell, V>(
                shell =>
                {
                    var frame = GetOrCreateWindowFrame(shell, typeof(T).GUID);
                    frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docviewObject);

                    return (V)docviewObject;
                });

            return docView;
        }

        private static IVsWindowFrame GetOrCreateWindowFrame(IVsUIShell shell, Guid toolWindowId)
        {
            // We want VS to ask the package to create the tool window if it doesn't already exist
            const uint flags = (uint)__VSFINDTOOLWIN.FTW_fForceCreate;

            var hr = shell.FindToolWindow(flags, toolWindowId, out var windowFrame);
            Debug.Assert(ErrorHandler.Succeeded(hr), $"Failed to find tool window. Guid: {toolWindowId}, hr: {hr} ");

            if (ErrorHandler.Succeeded(hr))
            {
                return windowFrame;
            }
            return null;
        }
    }
}
