//-----------------------------------------------------------------------
// <copyright file="SonarLintIntegrationPackage.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(CommonGuids.Package)]
    [ProvideBindingPath]
    [ProvideAutoLoad(CommonGuids.PackageActivation)]
    [ProvideOptionPage(typeof(OptionsPage), OptionsPage.CategoryName, OptionsPage.PageName, 901, 902, false, 903)]
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
        private SonarAnalyzerDeactivationManager sonarAnalyzerDeactivationManager;

        protected override void Initialize()
        {
            base.Initialize();
            this.InitializeSqm();

            IServiceProvider serviceProvider = this;

            this.usageAnalyzer = new BoundSolutionAnalyzer(serviceProvider);
            this.commandManager = new PackageCommandManager(serviceProvider);
            this.sonarAnalyzerDeactivationManager = new SonarAnalyzerDeactivationManager(serviceProvider);

            this.commandManager.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.usageAnalyzer?.Dispose();
                this.usageAnalyzer = null;

                this.sonarAnalyzerDeactivationManager?.Dispose();
                this.sonarAnalyzerDeactivationManager = null;
            }
        }
    }
}
