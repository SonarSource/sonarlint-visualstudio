/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

[Export(typeof(IVCXCompilationDatabaseProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class VCXCompilationDatabaseProvider(
    IVsUIServiceOperation uiServiceOperation,
    IVCXCompilationDatabaseStorage storage,
    ILogger logger,
    IThreadHandling threadHandling)
    : IVCXCompilationDatabaseProvider
{
    private readonly IFileConfigProvider fileConfigProvider = new FileConfigProvider(logger);

    public string CreateOrNull(string filePath)
    {
        IFileConfig fileConfig = null;
        uiServiceOperation.Execute<SDTE, DTE2>(dte =>
        {
            threadHandling.ThrowIfNotOnUIThread();

            var projectItem = dte.Solution.FindProjectItem(filePath);
            if (projectItem == null)
            {
                return;
            }

            fileConfig = fileConfigProvider.Get(projectItem, filePath, null);
        });

        if (fileConfig is null)
        {
            return null;
        }

        return storage.CreateDatabase(fileConfig);
    }
}
