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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.ServerSentEvents;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{

    /*
        This package will auto-load when a bound solution is opened and the server connection
        has been established.

        Currently, the only job of this package is to start the taint sync process running.
        This is being done in a different package to the one that provides the taint tool window
        to avoid threading issues on package initialization (if the sync process and taint window
        are provisioned in the same package, the sync process can trigger a call back to the package
        to display the tool window before the package has finished initialising, causing VS to lock up).
    */

    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(BoundSolutionUIContext.GuidString, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid("EEEB81FA-D6C3-438D-B29E-9FAAC91471CC")]
    public sealed class TaintSyncPackage : AsyncPackage
    {
        private ITaintIssuesBindingMonitor bindingMonitor;
        private ITaintServerEventsListener taintServerEventsListener;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var logger = componentModel.GetService<ILogger>();

            logger.WriteLine(TaintResources.SyncPackage_Initializing);

            bindingMonitor = componentModel.GetService<ITaintIssuesBindingMonitor>();

            await componentModel.GetService<ITaintIssuesSynchronizer>().SynchronizeWithServer();

            taintServerEventsListener = componentModel.GetService<ITaintServerEventsListener>();
            taintServerEventsListener.ListenAsync().Forget();

            logger.WriteLine(TaintResources.SyncPackage_Initialized);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bindingMonitor?.Dispose();
                taintServerEventsListener?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
