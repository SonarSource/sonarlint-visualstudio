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

using Microsoft.CodeAnalysis.Options;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class SolutionAnalysisRequester : ISolutionAnalysisRequester
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWorkspaceConfigurator workspaceConfigurator;
        private readonly OptionKey? fullSolutionAnalysisOptionKey;

        public SolutionAnalysisRequester(IServiceProvider serviceProvider, IWorkspaceConfigurator workspaceConfigurator)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (workspaceConfigurator == null)
            {
                throw new ArgumentNullException(nameof(workspaceConfigurator));
            }

            this.serviceProvider = serviceProvider;
            this.workspaceConfigurator = workspaceConfigurator;
            this.fullSolutionAnalysisOptionKey = FindFullSolutionAnalysisOptionKey(serviceProvider, workspaceConfigurator);
        }

        /// <summary>
        /// Forces a full solution reanalysis
        /// </summary>
        public void ReanalyzeSolution()
        {
            if (!fullSolutionAnalysisOptionKey.HasValue)
            {
                VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.MissingRuntimeOptionsInWorkspace);
                return;
            }

            // Flipping the flag twice to use the original setting and still force the re-analysis
            this.workspaceConfigurator.ToggleBooleanOptionKey(this.fullSolutionAnalysisOptionKey.Value); // toggle once
            this.workspaceConfigurator.ToggleBooleanOptionKey(this.fullSolutionAnalysisOptionKey.Value); // toggle back
        }

        public static OptionKey? FindFullSolutionAnalysisOptionKey(IServiceProvider serviceProvider, IWorkspaceConfigurator workspaceConfigurator)
        {
            // FullSolutionAnalysis options are defined here:
            // https://github.com/dotnet/roslyn/blob/614299ff83da9959fa07131c6d0ffbc58873b6ae/src/Workspaces/Core/Portable/Shared/RuntimeOptions.cs
            var roslynRuntimeOptions = RoslynRuntimeOptions.Resolve(serviceProvider);
            if (roslynRuntimeOptions == null)
            {
                return null;
            }

            IOption option = workspaceConfigurator.FindOptionByName(roslynRuntimeOptions.RuntimeOptionsFeatureName, roslynRuntimeOptions.FullSolutionAnalysisOptionName);
            Debug.Assert(option != null);

            return new OptionKey(option);
        }


    }
}
