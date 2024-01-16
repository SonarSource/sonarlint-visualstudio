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


/**************************************************************
 * DEBUG-only package to help with local debugging of SLVS 2019
 **************************************************************
 *
 * We can't open or build the SLVS solution in VS2019 any more (we use new
 * MSBuild features that are not supported).
 * 
 * 1. Close all VS2022 instances
 * 2. Open a VS *2022* Developer Command Prompt
 * 2. Build SLVS using the following command:
 *
 *      msbuild /p:vstargetversion=2019 /t:restore,rebuild
 * 
 * 3. Install the resulting SLVS2019 vsix locally.
 * 4. Re-open the SLVS solution in VS2022
 * 5. Open a VS *2019* Developer Command Prompt
 * 6. Set the required environment variable using the following command:
 * 
 *   set SLVSDEBUG=anything
 *
 * 7. Start VS2019 from the command line (devenv.exe)
 * 
 * When VS loads this package and hits the Debugger.Launch call Windows
 * will pop up a dialogue asking if you want to attach a debugger.
 * Select the instance of VS2022 you opened in step (4) above.
 * 
 * The SLVS 2019 pdbs should match the assemblies being used by VS2019, so
 * you should be able to debug normally i.e. setting breaking point,
 * stepping through code etc.
 *
 * If this package is loaded too late, you can put System.Debugger.Launch
 * calls before the specific code you want to debug.
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
