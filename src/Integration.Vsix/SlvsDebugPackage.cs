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
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

#if DEBUG

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(SlvsDebugPackage.PackageGuidString)]
    public sealed class SlvsDebugPackage : AsyncPackage
    {
        /// <summary>
        /// PackageLoadFailureReproPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "1E75DD9C-4816-4BF3-B5DC-38BD42E7ABA3";

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Stop if the SLVSDEVBUG environment variable has any value at all
            if (System.Environment.GetEnvironmentVariable("SLVSDEBUG") != null)
            {
                // We don't strictly need to be on the UI thread to launch the debugger,
                // but blocking the UI thread should pause VS so we can attached earlier
                // in the initialization
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                System.Diagnostics.Debugger.Launch();
            }
        }

        #endregion
    }
}

#endif
