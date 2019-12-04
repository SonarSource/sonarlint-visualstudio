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
using System.Collections.Generic;
using System.Threading;
using EnvDTE;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Messages;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    /// <summary>
    /// The new connected mode does not install NuGet analyzers. This class is
    /// a no-op implementation that will be plugged into the <see cref="BindingWorkflow"/>
    /// so the NuGet-related steps in the binding process do nothing.
    /// </summary>
    internal class NoOpNuGetBindingOperation : INuGetBindingOperation
    {
        private readonly ILogger logger;

        public NoOpNuGetBindingOperation(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            this.logger = logger;
        }

        public void PrepareOnUIThread()
        {
            // Nothing to do
        }

        /// <summary>
        /// Will install the NuGet packages for the current managed projects.
        /// The packages that will be installed will be based on the information from <see cref="Analyzer.GetRequiredNuGetPackages"/>
        /// and is specific to the <see cref="RuleSet"/>.
        /// </summary>
        public bool InstallPackages(ISet<Project> projectsToBind, IProgress<FixedStepsProgress> progress, CancellationToken token)
        {
            // Nothing to do - just return success
            this.logger.WriteLine(Strings.Bind_NuGetAnalyzersNoLongerInstalled);
            return true;
        }

        public bool ProcessExport(Language language, RoslynExportProfileResponse exportProfileResponse)
        {
            // Nothing to do - just return success
            return true;
        }
    }
}
