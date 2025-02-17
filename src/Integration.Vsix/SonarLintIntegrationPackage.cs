/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

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
    [InstalledProductRegistration("#110", "#112", "8.13.0.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), "SonarQube for Visual Studio", GeneralOptionsDialogPage.PageName, 901, 902, false, 903)]
    [ProvideOptionPage(typeof(OtherOptionsDialogPage), "SonarQube for Visual Studio", OtherOptionsDialogPage.PageName, 901, 904, true)]
    [ProvideUIContextRule(CommonGuids.PackageActivation, "SonarLintIntegrationPackageActivation",
        "(HasCSProj | HasVBProj)",
        new string[] { "HasCSProj", "HasVBProj" },
        new string[] { "SolutionHasProjectCapability:CSharp", "SolutionHasProjectCapability:VB" }
    )]
    [SuppressMessage("Reliability",
        "S2931:Classes with \"IDisposable\" members should implement \"IDisposable\"",
        Justification = "By-Design. The base class exposes a Dispose override in which the disposable instances will be disposed",
        Scope = "type",
        Target = "~T:SonarLint.VisualStudio.Integration.Vsix.SonarLintIntegrationPackage")]
    [ExcludeFromCodeCoverage]
    public class SonarLintIntegrationPackage : AsyncPackage
    {
        // Note: we don't currently have any tests for this class so we don't need to inject
        // thread handling wrapper. However, we'll still use it so our threading code is
        // consistent.
        private readonly IThreadHandling threadHandling = ThreadHandling.Instance;

        private PackageCommandManager commandManager;

        private ILogger logger;
        private IRoslynSettingsFileSynchronizer roslynSettingsFileSynchronizer;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await InitOnUIThreadAsync();
        }

        private async Task InitOnUIThreadAsync()
        {
            try
            {
                logger = await this.GetMefServiceAsync<ILogger>();
                Debug.Assert(logger != null, "MEF composition error - failed to retrieve a logger");
                logger.WriteLine(Strings.SL_Initializing);

                IServiceProvider serviceProvider = this;

                commandManager = new PackageCommandManager(serviceProvider.GetService<IMenuCommandService>());

                commandManager.Initialize(
                    serviceProvider.GetMefService<IProjectPropertyManager>(),
                    serviceProvider.GetMefService<IOutputWindowService>(),
                    serviceProvider.GetMefService<IShowInBrowserService>(),
                    ShowOptionPage,
                    serviceProvider.GetMefService<IConnectedModeServices>(),
                    serviceProvider.GetMefService<IConnectedModeBindingServices>(),
                    serviceProvider.GetMefService<IConnectedModeUIManager>());

                // make sure roslynSettingsFileSynchronizer is initialized
                roslynSettingsFileSynchronizer = await this.GetMefServiceAsync<IRoslynSettingsFileSynchronizer>();
                Debug.Assert(threadHandling.CheckAccess(), "Still expecting to be on the UI thread");

                logger.WriteLine(Strings.SL_InitializationComplete);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
                logger.WriteLine(Strings.SL_ERROR, ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                roslynSettingsFileSynchronizer?.Dispose();
                roslynSettingsFileSynchronizer = null;
            }
        }
    }
}
