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
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.CFamily.PreCompiledHeaders;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public sealed class SonarLintDaemonPackage : AsyncPackage
    {
        public const string PackageGuidString = "6f63ab5a-5ab8-4a0d-9914-151911885966";

        public const string CommandSetGuidString = "1F83EA11-3B07-45B3-BF39-307FD4F42194";

        private ILogger logger;
        private IPreCompiledHeadersEventListener cFamilyPreCompiledHeadersEventListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarLintDaemonPackage"/> class.
        /// </summary>
        public SonarLintDaemonPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        protected override System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            JoinableTaskFactory.RunAsync(InitAsync);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task InitAsync()
        {
            try
            {
                logger = await this.GetMefServiceAsync<ILogger>();
                logger.WriteLine(Resources.Strings.Daemon_Initializing);

                await DisableRuleCommand.InitializeAsync(this, logger);
                await CFamilyReproducerCommand.InitializeAsync(this, logger);

                cFamilyPreCompiledHeadersEventListener = await this.GetMefServiceAsync<IPreCompiledHeadersEventListener>();

                LegacyInstallationCleanup.CleanupDaemonFiles(logger);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger?.WriteLine(Resources.Strings.ERROR_InitializingDaemon, ex);
            }
            logger?.WriteLine(Resources.Strings.Daemon_InitializationComplete);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                cFamilyPreCompiledHeadersEventListener?.Dispose();
                cFamilyPreCompiledHeadersEventListener = null;
            }
        }

        #endregion
    }
}
