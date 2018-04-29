/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Analyzes the solution on build in order to determine if has a SonarQube/Cloud configuration file
    /// and log that using <see cref="ITelemetryLogger"/>.
    /// </summary>
    internal sealed class BoundSolutionAnalyzer : IDisposable
    {
        private readonly IServiceProvider serviceProvider;

        internal const string BindingConfigurationSearchPattern = "*.sqconfig;*.slconfig";

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
            var dte = this.serviceProvider.GetService<DTE>();
            string fullSolutionPath = dte.Solution?.FullName;
            if (string.IsNullOrWhiteSpace(fullSolutionPath))
            {
                Debug.Fail("Solution expected since building...");
                return;
            }

            string solutionDir = Path.GetDirectoryName(fullSolutionPath);
            string[] existingFiles = GetFiles(solutionDir, Constants.LegacySonarQubeManagedFolderName, "*.sqconfig")
                ?? GetFiles(solutionDir, Constants.SonarlintManagedFolderName, "*.slconfig");
            
            if (existingFiles?.Length > 0)
            {
                this.serviceProvider.GetMefService<ITelemetryLogger>()?.ReportEvent(TelemetryEvent.BoundSolutionDetected);
            }
        }

        private static string[] GetFiles(string rootDirectory, string folder, string pattern)
        {
            string searchDir = Path.Combine(rootDirectory, folder);
            if (!Directory.Exists(searchDir))
            {
                // Returning null so the caller can use the null-coalesce operator to check for other directories
                return null;
            }
            return Directory.GetFiles(searchDir, pattern, SearchOption.TopDirectoryOnly);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        private void Dispose(bool disposing)
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
