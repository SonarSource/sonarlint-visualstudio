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

using System.Collections.Generic;
using System.Threading;
using EnvDTE;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Messages;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Encapsulate the workflow steps to install NuGet packages during binding
    /// </summary>
    public interface INuGetBindingOperation
    {
        void PrepareOnUIThread();

        /// <summary>
        /// Returns true if the operation completed succcessfully, otherwise false
        /// </summary>
        bool InstallPackages(ISet<Project> projectsToBind, IProgressStepExecutionEvents notificationEvents, CancellationToken token);

        /// <summary>
        /// Extracts any required information from the supplied Roslyn export profile
        /// </summary>
        /// <returns>True if processing was successful, otherwise false</returns>
        bool ProcessExport(Language language, RoslynExportProfileResponse exportProfileResponse);
    }
}
