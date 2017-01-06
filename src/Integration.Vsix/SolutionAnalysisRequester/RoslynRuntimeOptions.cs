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

using SonarLint.VisualStudio.Integration.Vsix.Resources;
using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public abstract class RoslynRuntimeOptions
    {
        public static readonly RoslynRuntimeOptions VS2015 = new Vs2015RoslynRuntimeOptions();
        public static readonly RoslynRuntimeOptions VS2017 = new Vs2017RoslynRuntimeOptions();

        public abstract string RuntimeOptionsFeatureName { get; }
        public abstract string FullSolutionAnalysisOptionName { get; }

        public static RoslynRuntimeOptions Resolve(IServiceProvider serviceProvider)
        {
            var visualStudioVersion = serviceProvider.GetService<EnvDTE.DTE>()?.Version;
            switch (visualStudioVersion)
            {
                case VisualStudioConstants.VS2015VersionNumber:
                    return VS2015;
                case VisualStudioConstants.VS2017VersionNumber:
                    return VS2017;
                default:
                    VsShellUtils.WriteToSonarLintOutputPane(serviceProvider, Strings.InvalidVisualStudioVersion, visualStudioVersion);
                    return null;
            }
        }

        private class Vs2015RoslynRuntimeOptions : RoslynRuntimeOptions
        {
            public override string RuntimeOptionsFeatureName { get; } = "Runtime";
            public override string FullSolutionAnalysisOptionName { get; } = "Full Solution Analysis";
        }

        private class Vs2017RoslynRuntimeOptions : RoslynRuntimeOptions
        {
            public override string RuntimeOptionsFeatureName { get; } = "RuntimeOptions";
            public override string FullSolutionAnalysisOptionName { get; } = "FullSolutionAnalysis";
        }
    }
}
