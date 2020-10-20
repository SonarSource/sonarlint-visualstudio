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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE
{
    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("5C38BF5B-F6D1-4232-A5B5-5664B6A19A91")]
    // We want to start listening as soon as the IDE starts.
    // Note: in VS2019, this will be when the new modal start page appears.
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    internal sealed class OpenInIDEPackage : AsyncPackage
    {
        // Note: the range of ports used is common across SonarLint implementations in all IDEs and
        // must match those checked by SonarQube/SonarCloud.
        private const int StartPort = 64120;
        private const int EndPort = 64123;

        private HttpListener listener;

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            IComponentModel componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var logger = componentModel.GetExtensions<ILogger>().First();

            IListenerFactory listenerFactory = new ListenerFactory(logger);
            listener = listenerFactory.Create(StartPort, EndPort);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                ((IDisposable)listener).Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
