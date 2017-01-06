/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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
