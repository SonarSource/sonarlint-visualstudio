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
    internal class SolutionAnalysisRequester : ISolutionAnalysisRequester
    {
        internal const string OptionNameFullSolutionAnalysis = "Full Solution Analysis";
        internal const string OptionFeatureRuntime = "Runtime";

        private readonly Workspace workspace;
        private readonly IServiceProvider serviceProvider;

        private object optionService;
        private readonly Option<bool> fullSolutionAnalysisOption;
        private MethodInfo optionServiceSetOptionsMethod;
        private MethodInfo optionServiceGetOptionsMethod;

        internal /* for testing */ SolutionAnalysisRequester(IServiceProvider serviceProvider, Workspace workspace,
            Option<bool> fullSolutionAnalysisOption)
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
            this.fullSolutionAnalysisOption = fullSolutionAnalysisOption;

            this.FindOptionService();
        }

        public SolutionAnalysisRequester(IServiceProvider serviceProvider, Workspace workspace)
            : this(serviceProvider, workspace, GetFullSolutionAnalysisOption(serviceProvider))
        {
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

        private static Option<bool> GetFullSolutionAnalysisOption(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            string codeAnalysisDllPath = typeof(Accessibility).Assembly.Location;
            if (string.IsNullOrEmpty(codeAnalysisDllPath))
            {
                Debug.Fail("Microsoft.CodeAnalysis.dll could not be located");
                return null;
            }

            FileInfo codeAnalysisAssembly = new FileInfo(codeAnalysisDllPath);
            if (!codeAnalysisAssembly.Exists)
            {
                Debug.Fail("Microsoft.CodeAnalysis.dll could not be located");
                return null;
            }

            string codeAnalysisFolderPath = codeAnalysisAssembly.Directory.FullName;
            const string codeAnalysisFeaturesDllName = "Microsoft.CodeAnalysis.Features.dll";

            // There's no public type in the DLL, so we try finding it next to Microsoft.CodeAnalysis
            string path = Path.Combine(codeAnalysisFolderPath, codeAnalysisFeaturesDllName);

            if (!File.Exists(path))
            {
                VsShellUtils.WriteToGeneralOutputPane(serviceProvider, Strings.MissingResourceAtLocation,
                    codeAnalysisFeaturesDllName, codeAnalysisFolderPath);
                return null;
            }

            // This is only part of Visual Studio 2015 Update 2
            Option<bool> option = (Option<bool>)Assembly.LoadFile(path)
                .GetType("Microsoft.CodeAnalysis.Shared.Options.RuntimeOptions", false)
                ?.GetField("FullSolutionAnalysis")
                ?.GetValue(null);

            Debug.Assert(option != null, "RuntimeOptions is not found");
            Debug.Assert(option.Name == OptionNameFullSolutionAnalysis, OptionNameFullSolutionAnalysis + " option name changed to " + option.Name);
            Debug.Assert(option.Name == OptionFeatureRuntime, OptionFeatureRuntime + " option feature changed to " + option.Feature);

            return option;
        }

        internal /* for testing */ void FlipFullSolutionAnalysisFlag()
        {
            if (this.optionService == null ||
                this.fullSolutionAnalysisOption == null ||
                this.optionServiceGetOptionsMethod == null ||
                this.optionServiceSetOptionsMethod == null)
            {
                return;
            }

            var options = this.optionServiceGetOptionsMethod.Invoke(this.optionService, null) as OptionSet;
            if (options == null)
            {
                return;
            }

            var optionValue = options.GetOption(this.fullSolutionAnalysisOption);
            var newOptions = options.WithChangedOption(this.fullSolutionAnalysisOption, !optionValue);
            this.optionServiceSetOptionsMethod.Invoke(this.optionService, new object[] { newOptions });
        }

        // This method is only required for testing
        internal bool GetOptionValue()
        {
            var options = this.optionServiceGetOptionsMethod.Invoke(this.optionService, null) as OptionSet;
            return options.GetOption(this.fullSolutionAnalysisOption);
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
