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

                this.sonarAnalyzerManager?.Dispose();
                this.sonarAnalyzerManager = null;
            }
        }
    }
}
