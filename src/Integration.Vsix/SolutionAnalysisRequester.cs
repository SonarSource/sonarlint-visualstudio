//-----------------------------------------------------------------------
// <copyright file="SolutionAnalysisRequester.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class SolutionAnalysisRequester
    {
        private readonly Workspace workspace;
        private readonly IServiceProvider serviceProvider;

        private object optionService;
        private Option<bool> option;
        private MethodInfo optionServiceSetOptionsMethod;
        private MethodInfo optionServiceGetOptionsMethod;

        public SolutionAnalysisRequester(IServiceProvider serviceProvider, Workspace workspace)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            this.serviceProvider = serviceProvider;
            this.workspace = workspace;
            this.FindOptionService();
            this.FindFullSolutionAnalysisOption();
        }

        private void FindOptionService()
        {
            const string optionServiceTypeName = "Microsoft.CodeAnalysis.Options.IOptionService";
            const string CodeAnalysisWorkspacesAssemblyName = "Microsoft.CodeAnalysis.Workspaces";

            Type optionServiceType = typeof(PerLanguageOption<int>).Assembly.GetType(optionServiceTypeName, false);
            if (optionServiceType == null)
            {
                VsShellUtils.WriteToGeneralOutputPane(this.serviceProvider, Strings.MissingResourceAtLocation,
                    optionServiceTypeName, CodeAnalysisWorkspacesAssemblyName);
                Debug.Fail($"{optionServiceTypeName} could not be found in {CodeAnalysisWorkspacesAssemblyName}");
                return;
            }

            this.optionService = this.workspace.Services.GetType()
                ?.GetMethod("GetService")
                ?.MakeGenericMethod(optionServiceType)
                ?.Invoke(workspace.Services, null);

            Debug.Assert(this.optionService != null, "Option service is null");

            this.optionServiceSetOptionsMethod = optionServiceType?.GetMethod("SetOptions");
            this.optionServiceGetOptionsMethod = optionServiceType?.GetMethod("GetOptions");

            Debug.Assert(this.optionServiceSetOptionsMethod != null, "IOptionService.SetOptions method is not found");
            Debug.Assert(this.optionServiceGetOptionsMethod != null, "IOptionService.GetOptions method is not found");
        }

        private void FindFullSolutionAnalysisOption()
        {
            string codeAnalysisDllPath = typeof(Accessibility).Assembly.Location;
            if (string.IsNullOrEmpty(codeAnalysisDllPath))
            {
                Debug.Fail("Microsoft.CodeAnalysis.dll could not be located");
                return;
            }

            FileInfo codeAnalysisAssembly = new FileInfo(codeAnalysisDllPath);
            if (!codeAnalysisAssembly.Exists)
            {
                Debug.Fail("Microsoft.CodeAnalysis.dll could not be located");
                return;
            }

            string codeAnalysisFolderPath = codeAnalysisAssembly.Directory.FullName;
            const string codeAnalysisFeaturesDllName = "Microsoft.CodeAnalysis.Features.dll";

            // There's no public type in the DLL, so we try finding it next to Microsoft.CodeAnalysis
            string path = Path.Combine(codeAnalysisFolderPath, codeAnalysisFeaturesDllName);

            if (!File.Exists(path))
            {
                VsShellUtils.WriteToGeneralOutputPane(this.serviceProvider, Strings.MissingResourceAtLocation,
                    codeAnalysisFeaturesDllName, codeAnalysisFolderPath);
                return;
            }

            // This is only part of Visual Studio 2015 Update 2
            this.option = (Option<bool>)Assembly.LoadFile(path)
                .GetType("Microsoft.CodeAnalysis.Shared.Options.RuntimeOptions", false)
                ?.GetField("FullSolutionAnalysis")
                ?.GetValue(null);

            Debug.Assert(this.option != null, "RuntimeOptions is not found");
        }

        private void FlipFullSolutionAnalysisFlag()
        {
            if (this.optionService == null ||
                this.option == null ||
                this.optionServiceGetOptionsMethod == null ||
                this.optionServiceSetOptionsMethod == null)
            {
                return;
            }

            var options = this.optionServiceGetOptionsMethod.Invoke(optionService, null) as OptionSet;
            if (options == null)
            {
                return;
            }

            var optionValue = options.GetOption(this.option);
            var newOptions = options.WithChangedOption(this.option, !optionValue);
            this.optionServiceSetOptionsMethod.Invoke(this.optionService, new object[] { newOptions });
        }

        /// <summary>
        /// Forces a full solution reanalysis
        /// </summary>
        public void ReanalyzeSolution()
        {
            // Flipping the flag twice to use the original setting and still force the re-analysis
            this.FlipFullSolutionAnalysisFlag();
            this.FlipFullSolutionAnalysisFlag();
        }
    }
}
