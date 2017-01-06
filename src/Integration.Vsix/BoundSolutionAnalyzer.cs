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

using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Analyzes the solution on build in order to determine if it has SonarQube rulesets
    /// and log that using <see cref="ITelemetryLogger"/>.
    /// </summary>
    internal class BoundSolutionAnalyzer : IDisposable
    {
        private readonly IServiceProvider serviceProvider;

        // Don't use the constants from the referenced project in order to not accidentally load things that were not loaded previously
        internal const string SonarQubeFilesFolder = "SonarQube";
        internal const string SonarQubeSolutionBindingConfigurationSearchPattern = "*.sqconfig";

        public BoundSolutionAnalyzer(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
            KnownUIContexts.SolutionBuildingContext.UIContextChanged += this.OnSolutionBuilding;
            if (KnownUIContexts.SolutionBuildingContext.IsActive)
            {
                this.OnSolutionBuilding();
            }
        }

        private void OnSolutionBuilding(object sender, UIContextChangedEventArgs e)
        {
            if (e.Activated)
            {
                this.OnSolutionBuilding();
            }
        }

        private void OnSolutionBuilding()
        {
            var dte = this.serviceProvider.GetService(typeof(DTE)) as DTE;
            string fullSolutionPath = dte.Solution?.FullName;
            if (string.IsNullOrWhiteSpace(fullSolutionPath))
            {
                Debug.Fail("Solution expected since building...");
                return;
            }

            string expectedSonarQubeDirectory = Path.Combine(Path.GetDirectoryName(fullSolutionPath), SonarQubeFilesFolder);
            if (!Directory.Exists(expectedSonarQubeDirectory))
            {
                return; //Bail out no need to analyze the projects
            }

            string[] existingFiles = Directory.GetFiles(expectedSonarQubeDirectory, SonarQubeSolutionBindingConfigurationSearchPattern, SearchOption.TopDirectoryOnly);
            if (existingFiles.Length > 0)
            {
                var componentModel = this.serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
                var telemetryLogger = componentModel?.GetExtensions<ITelemetryLogger>().SingleOrDefault();
                if (telemetryLogger == null)
                {
                    Debug.Fail("Failed to find ITelemetryLogger");
                    return;
                }

                telemetryLogger.ReportEvent(TelemetryEvent.BoundSolutionDetected);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    KnownUIContexts.SolutionBuildingContext.UIContextChanged -= this.OnSolutionBuilding;
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
