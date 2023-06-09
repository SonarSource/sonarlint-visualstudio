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

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    /// <summary>
    /// Return the path of solution's binding configuration file when in connected mode.
    /// </summary>
    internal interface IUnintrusiveBindingPathProvider
    {
        string Get();
    }

    [Export(typeof(IUnintrusiveBindingPathProvider))]
    internal class UnintrusiveBindingPathProvider : IUnintrusiveBindingPathProvider
    {
        private readonly IVsSolution solution;

        private readonly string SLVSRootBindingFolder;

        [ImportingConstructor]
        public UnintrusiveBindingPathProvider([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider,
            IThreadHandling threadHandling)
            : this(serviceProvider, threadHandling, EnvironmentVariableProvider.Instance)
        {
        }

        internal /* for testing */ UnintrusiveBindingPathProvider(IServiceProvider serviceProvider,
            IThreadHandling threadHandling,
            IEnvironmentVariableProvider environmentVariables)
        {
            SLVSRootBindingFolder = Path.Combine(environmentVariables.GetSLVSAppDataRootPath(), "Bindings");

            IVsSolution slnService = null;
            threadHandling.RunOnUIThreadSync(() => slnService = serviceProvider.GetService<SVsSolution, IVsSolution>());
            solution = slnService;
        }

        public string Get()
        {
            // If there isn't an open solution the returned hresult will indicate an error
            // and the returned solution name will be null. We'll just ignore the hresult.
            solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out var fullSolutionName);

            return GetConnectionFilePath(fullSolutionName as string);
        }

        private string GetConnectionFilePath(string solutionFilePath)
        {
            if (solutionFilePath == null)
            {
                return null;
            }

            // The path must match the one in the SonarLintTargets.xml file that is dropped in
            // the MSBuild ImportBefore folder i.e.
            //   $(APPDATA)\SonarLint for Visual Studio\\Bindings\\$(SolutionName)_$(SolutionDir.GetHashCode())\binding.config

            var solutionFolder = Path.GetDirectoryName(solutionFilePath);
            var solutionName = Path.GetFileNameWithoutExtension(solutionFilePath);

            return Path.Combine(SLVSRootBindingFolder, $"{solutionName}_{solutionFolder.GetHashCode()}", "binding.config");
        }
    }
}
