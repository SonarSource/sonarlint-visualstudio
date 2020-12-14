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
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.Security.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Package
{
    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("FE058062-65D9-4DE1-9690-4EE58A128E78")]
    [ProvideToolWindow(typeof(HotspotsToolWindow), MultiInstances = false, Transient = true, Style = VsDockStyle.Tabbed, Window = Commands.Constants.VsWindowKindErrorList, Width = 700, Height = 250)]
    public sealed class HotspotsPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await ShowToolWindowCommand.CreateAsync(this,
                new CommandID(Constants.CommandSetGuid, Constants.HotspotsToolWindowCommandId),
                HotspotsToolWindow.ToolWindowId);
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(HotspotsToolWindow))
            {
                return new HotspotsToolWindow(this);
            }

            return base.InstantiateToolWindow(toolWindowType);
        }
    }
}
