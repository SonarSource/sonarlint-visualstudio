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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core.Telemetry;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ExcludeFromCodeCoverage] // Simple bootstrapper class relying on Visual Studio
    public sealed class SonarLintTelemetryPackage : AsyncPackage
    {
        public const string PackageGuidString = "4E057B4B-E2B8-490D-95D8-2A1A4E7ACAED";

        private ITelemetryManager telemetryManager;

        protected async override System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Debug.Assert(!ThreadHelper.CheckAccess(), "Not expecting the package to be initialized on the UI thread");

            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await InitOnUIThreadAsync();
        }

        private async System.Threading.Tasks.Task InitOnUIThreadAsync()
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expecting to be on the UI thread");

            var logger = await this.GetMefServiceAsync<ILogger>();

            // HACK: the telemetry manager has to be imported on the UI thread because
            // of a complicated chain of transitive dependencies:
            // TelemetryManager -> IActiveSolutionBoundTracker -> IBindingConfiguration -> IHost.
            // The host expects to be initialized on the UI thread.
            // The ui and non-ui parts of the host should be split into separate classes.
            try
            {
                logger.WriteLine(Resources.Strings.Telemetry_Initializing);
                telemetryManager = await this.GetMefServiceAsync<ITelemetryManager>();
                logger.WriteLine(Resources.Strings.Telemetry_InitializationComplete);

                if (await IsSolutionFullyOpenedAsync())
                {
                    telemetryManager.Update();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
                logger.WriteLine(Resources.Strings.Telemetry_ERROR, ex.Message);
            }
        }

        private async System.Threading.Tasks.Task<bool> IsSolutionFullyOpenedAsync()
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expecting to be called on the UI thread");
            var solution = await this.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            object isLoaded;
            var hresult = solution.GetProperty((int)__VSPROPID4.VSPROPID_IsSolutionFullyLoaded, out isLoaded);

            return ErrorHandler.Succeeded(hresult) && (isLoaded as bool?) == true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            (telemetryManager as IDisposable)?.Dispose();
            telemetryManager = null;
        }
    }
}
