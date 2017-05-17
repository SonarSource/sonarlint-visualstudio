/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    // Register this class as a VS package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(CommonGuids.Package)]
    [ProvideBindingPath]
    // Specify when to load the extension (GUID can be found in Microsoft.VisualStudio.VSConstants.UICONTEXT)
    [ProvideAutoLoad(CommonGuids.PackageActivation)]
    // Register the information needed to show the package in the Help/About dialog of VS.
    // NB: The version is automatically updated by the ChangeVersion.proj
    [InstalledProductRegistration("#110", "#112", "3.0.0.0", IconResourceID = 400)]
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
    public partial class SonarLintIntegrationPackage : Package
    {
        private BoundSolutionAnalyzer usageAnalyzer;
        private PackageCommandManager commandManager;
        private SonarAnalyzerManager sonarAnalyzerManager;

        protected override void Initialize()
        {
            base.Initialize();
            this.InitializeSqm();

            IServiceProvider serviceProvider = this;

            this.sonarAnalyzerManager = new SonarAnalyzerManager(serviceProvider);
            this.usageAnalyzer = new BoundSolutionAnalyzer(serviceProvider);
            this.commandManager = new PackageCommandManager(serviceProvider);

            this.commandManager.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.usageAnalyzer?.Dispose();
                this.usageAnalyzer = null;
            }
        }
    }
}
