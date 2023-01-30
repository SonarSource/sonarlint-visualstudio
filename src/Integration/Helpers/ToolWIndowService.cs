﻿/*
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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IToolWindowService))]
    internal class ToolWindowService : IToolWindowService
    {
        private readonly IVsUIShell shell;

        [ImportingConstructor]
        public ToolWindowService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            shell = serviceProvider.GetService<SVsUIShell, IVsUIShell>();
            Debug.Assert(shell != null, "Failed to retrieve the IVsUIShell");
        }

        public void Show(Guid toolWindowId)
        {
            var frame = GetOrCreateWindowFrame(toolWindowId);
            frame?.Show();
        }

        public void EnsureToolWindowExists(Guid toolWindowId)
        {
            GetOrCreateWindowFrame(toolWindowId);
        }

        public V GetToolWindow<T, V>() where T : class
        {
            var frame = GetOrCreateWindowFrame(typeof(T).GUID);
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docView);

            return (V)docView;
        }

        private IVsWindowFrame GetOrCreateWindowFrame(Guid toolWindowId)
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
