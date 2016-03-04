//-----------------------------------------------------------------------
// <copyright file="SonarLintIntegrationPackage.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
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
    public partial class SonarLintIntegrationPackage : Package
    {
        private BoundSolutionAnalyzer usageAnalyzer;
        private PackageCommandManager commandManager;
        private SonarAnalyzerCollisionManager sonarAnalyzerCollisionManager;

        protected override void Initialize()
        {
            base.Initialize();
            this.InitializeSqm();

            IServiceProvider serviceProvider = this;

            this.usageAnalyzer = new BoundSolutionAnalyzer(serviceProvider);
            this.commandManager = new PackageCommandManager(serviceProvider);
            this.sonarAnalyzerCollisionManager = new SonarAnalyzerCollisionManager(serviceProvider);

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
