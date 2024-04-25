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
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots_List.HotspotsList;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security
{
    /*
        This is a simple package that just provides the basic VS plumbing for the security commands
        and tool windows.

        It doesn't provide any other services, and doesn't need to be auto-loadable. It will be loaded
        by VS only when needed i.e. when it has received a request to to invoke a command or display a
        tool window.
    */

    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("D7D54E08-45E1-49A6-AA53-AF1CFAA6EBDC")]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideToolWindow(typeof(HotspotsToolWindow), MultiInstances = false, Transient = false, // Note: Transient must false when using ProvideToolWindowVisibility
        Style = VsDockStyle.Tabbed, Window = VsWindowKindErrorList, Width = 700, Height = 250)]
    [ProvideToolWindowVisibility(typeof(HotspotsToolWindow), LocalHotspotIssuesExistUIContext.GuidString)]

    [ProvideToolWindow(typeof(OpenInIDEHotspotsToolWindow), MultiInstances = false, Transient = true, Style = VsDockStyle.Tabbed, Window = VsWindowKindErrorList, Width = 700, Height = 250)]

    [ProvideToolWindow(typeof(TaintToolWindow), MultiInstances = false, Transient = false, // Note: Transient must false when using ProvideToolWindowVisibility
        Style = VsDockStyle.Tabbed, Window = VsWindowKindErrorList, Width = 700, Height = 250)]
    [ProvideToolWindowVisibility(typeof(TaintToolWindow), TaintIssuesExistUIContext.GuidString)]
    public sealed class IssueVizSecurityPackage : AsyncPackage
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/dotnet/api/envdte80.windowkinds.vswindowkinderrorlist?view=visualstudiosdk-2019
        /// </summary>
        public const string VsWindowKindErrorList = "{D78612C7-9962-4B83-95D9-268046DAD23A}";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var logger = componentModel.GetService<ILogger>();
            logger.WriteLine(Resources.StartedPackageInitialization);

            // We're not storing references to the command handler instances.
            // We are relying on the fact that the command handler instance registers a
            // callback with the menu service to stop it from being garbage collected.
            await ShowToolWindowCommand.CreateAsync(this,
                new CommandID(Constants.CommandSetGuid, Constants.HotspotsToolWindowCommandId),
                HotspotsToolWindow.ToolWindowId);

            await ShowToolWindowCommand.CreateAsync(this,
                new CommandID(Constants.CommandSetGuid, Constants.OpenInIDEHotspotsToolWindowCommandId),
                OpenInIDEHotspotsToolWindow.ToolWindowId);

            await ShowToolWindowCommand.CreateAsync(this,
                new CommandID(Constants.CommandSetGuid, Constants.TaintToolWindowCommandId),
                TaintToolWindow.ToolWindowId);

            logger.WriteLine(Resources.FinishedPackageInitialization);
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(HotspotsToolWindow))
            {
                return new HotspotsToolWindow(this);
            }

            if (toolWindowType == typeof(OpenInIDEHotspotsToolWindow))
            {
                return new OpenInIDEHotspotsToolWindow(this);
            }

            if (toolWindowType == typeof(TaintToolWindow))
            {
                return new TaintToolWindow(this);
            }

            return base.InstantiateToolWindow(toolWindowType);
        }
    }
}
