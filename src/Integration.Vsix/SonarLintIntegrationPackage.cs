﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    // Register this class as a VS package.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CommonGuids.Package)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideBindingPath]
    // Specify when to load the extension (GUID can be found in Microsoft.VisualStudio.VSConstants.UICONTEXT)
    [ProvideAutoLoad(CommonGuids.PackageActivation, PackageAutoLoadFlags.BackgroundLoad)]
    // Register the information needed to show the package in the Help/About dialog of VS.
    // NB: The version is automatically updated by the ChangeVersion.proj
    [InstalledProductRegistration("#110", "#112", "6.0.0.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), "SonarLint", GeneralOptionsDialogPage.PageName, 901, 902, false, 903)]
    [ProvideOptionPage(typeof(OtherOptionsDialogPage), "SonarLint", OtherOptionsDialogPage.PageName, 901, 904, true)]
    [ProvideUIContextRule(CommonGuids.PackageActivation, "SonarLintIntegrationPackageActivation",
         "(HasCSProj | HasVBProj)",
        new string[] { "HasCSProj",
                       "HasVBProj" },
        new string[] { "SolutionHasProjectCapability:CSharp",
                       "SolutionHasProjectCapability:VB" }
    )]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
        "S2931:Classes with \"IDisposable\" members should implement \"IDisposable\"",
        Justification = "By-Design. The base class exposes a Dispose override in which the disposable instances will be disposed",
        Scope = "type",
        Target = "~T:SonarLint.VisualStudio.Integration.Vsix.SonarLintIntegrationPackage")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SonarLintIntegrationPackage : AsyncPackage
    {
        private PackageCommandManager commandManager;
        private SonarAnalyzerManager sonarAnalyzerManager;

        private ILogger logger;

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await InitOnUIThreadAsync();
        }

        private async System.Threading.Tasks.Task InitOnUIThreadAsync()
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "SonarLint package - expecteding to be called in the UI thread");

            try
            {
                logger = await this.GetMefServiceAsync<ILogger>();
                Debug.Assert(logger != null, "MEF composition error - failed to retrieve a logger");
                logger.WriteLine(Resources.Strings.SL_Initializing);

                IServiceProvider serviceProvider = this;

                var activeSolutionBoundTracker = await this.GetMefServiceAsync<IActiveSolutionBoundTracker>();
                var sonarQubeIssuesProvider = await this.GetMefServiceAsync<ISonarQubeIssuesProvider>();
                var workspace = await this.GetMefServiceAsync<VisualStudioWorkspace>();

                var vsSolution = serviceProvider.GetService<SVsSolution, IVsSolution>();
                this.sonarAnalyzerManager = new SonarAnalyzerManager(activeSolutionBoundTracker, workspace, vsSolution, logger, sonarQubeIssuesProvider);

                this.commandManager = new PackageCommandManager(serviceProvider.GetService<IMenuCommandService>());
                this.commandManager.Initialize(serviceProvider.GetMefService<ITeamExplorerController>(),
                    serviceProvider.GetMefService<IProjectPropertyManager>(),
                    serviceProvider.GetMefService<IProjectToLanguageMapper>());

                logger.WriteLine(Resources.Strings.SL_InitializationComplete);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
                logger.WriteLine(Resources.Strings.SL_ERROR, ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.sonarAnalyzerManager?.Dispose();
                this.sonarAnalyzerManager = null;
            }
        }
    }
}
