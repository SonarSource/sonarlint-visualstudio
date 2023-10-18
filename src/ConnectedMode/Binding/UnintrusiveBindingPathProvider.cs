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

using System.ComponentModel.Composition;
using System.IO;
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
        private readonly ISolutionInfoProvider solutionInfoProvider;

        private readonly string SLVSRootBindingFolder;

        [ImportingConstructor]
        public UnintrusiveBindingPathProvider(ISolutionInfoProvider solutionInfoProvider)
            : this(solutionInfoProvider, EnvironmentVariableProvider.Instance)
        {
        }

        internal /* for testing */ UnintrusiveBindingPathProvider(ISolutionInfoProvider solutionInfoProvider,
            IEnvironmentVariableProvider environmentVariables)
        {
            SLVSRootBindingFolder = Path.Combine(environmentVariables.GetSLVSAppDataRootPath(), "Bindings");
            this.solutionInfoProvider = solutionInfoProvider;
        }

        public string Get()
        {
            // The path must match the one in the SonarLintTargets.xml file that is dropped in
            // the MSBuild ImportBefore folder i.e.
            //   $(APPDATA)\SonarLint for Visual Studio\\Bindings\\$(SolutionName)\binding.config
            var solutionName = solutionInfoProvider.GetSolutionName();
            return Path.Combine(SLVSRootBindingFolder, $"{solutionName}", "binding.config");
        }
    }
}
