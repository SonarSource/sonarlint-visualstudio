/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    internal interface IMigrationSettingsProvider
    {
        /// <summary>
        /// Returns the data required for the migration process
        /// </summary>
        System.Threading.Tasks.Task<LegacySettings> GetAsync(string sonarProjectKey);
    }

    [Export(typeof(IMigrationSettingsProvider))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class MigrationSettingsProvider : IMigrationSettingsProvider
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public MigrationSettingsProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IThreadHandling threadHandling)
        {
            this.serviceProvider = serviceProvider;
            this.threadHandling = threadHandling;
        }

        public async System.Threading.Tasks.Task<LegacySettings> GetAsync(string sonarProjectKey)
        {
            // Required files:
            // Root folder                      = {solution folder}\.sonarlint

            // Note: we're returning partial paths
            // CSharp ruleset       = .sonarlint\{sonar project key}csharp.ruleset
            // CSharp addit files   = .sonarlint\{sonar project key}\CSharp\SonarLint.xml
            // VB.NET ruleset       = .sonarlint\{sonar project key}vb.ruleset
            // VB.NET addit files   = .sonarlint\{sonar project key}\VB\SonarLint.xml

            var solutionDir = await GetSolutionDirectoryAsync();
            var rootFolder = Path.Combine(solutionDir, ".sonarlint");

            return new LegacySettings(
                rootFolder,
                Path.Combine(".sonarlint", sonarProjectKey + Language.CSharp.Id + ".ruleset").ToLowerInvariant(),
                Path.Combine(".sonarlint", sonarProjectKey, Language.CSharp.Id, "SonarLint.xml"),
                Path.Combine(".sonarlint", sonarProjectKey + Language.VBNET.Id + ".ruleset").ToLowerInvariant(),
                Path.Combine(".sonarlint", sonarProjectKey, Language.VBNET.Id, "SonarLint.xml")
                );
        }

        private async System.Threading.Tasks.Task<string> GetSolutionDirectoryAsync()
        {
            string solutionDir = null;

            await threadHandling.RunOnUIThread(() => {
                var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
                // If there isn't an open solution the returned hresult will indicate an error
                // and the returned solution name will be null. We'll just ignore the hresult.
                solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out var solutionDirAsObject);
                solutionDir = solutionDirAsObject as string;
            });
            return solutionDir;
        }
    }
}
