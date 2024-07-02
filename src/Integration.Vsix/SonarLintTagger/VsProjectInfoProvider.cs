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
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix;

internal interface IVsProjectInfoProvider
{
    Task<(string projectName, Guid projectGuid)> GetDocumentProjectInfoAsync(ITextDocument document);
}

[Export(typeof(IVsProjectInfoProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class VsProjectInfoProvider : IVsProjectInfoProvider
{
    private readonly DTE2 dte;
    private readonly IVsSolution5 vsSolution;
    private readonly ILogger logger;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public VsProjectInfoProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        ILogger logger,
        IThreadHandling threadHandling)
    {
        dte = serviceProvider.GetService<SDTE, DTE2>();
        vsSolution = serviceProvider.GetService<SVsSolution, IVsSolution5>();
        this.logger = logger;
        this.threadHandling = threadHandling;
    }

    public async Task<(string projectName, Guid projectGuid)> GetDocumentProjectInfoAsync(ITextDocument document)
    {
        string projectName = null;
        var projectGuid = Guid.Empty;

        await threadHandling.RunOnUIThreadAsync(() =>
        {
            var documentFilePath = document.FilePath;
            var project = GetProject(documentFilePath);

            projectName = GetProjectName(project);
            projectGuid = GetProjectGuid(project, documentFilePath);
        });
        return (projectName, projectGuid);
    }

    private string GetProjectName(Project project)
    {
        return project?.Name ?? "{none}";
    }

    private Guid GetProjectGuid(Project project, string documentFilePath)
    {
        threadHandling.ThrowIfNotOnUIThread();
        
        try
        {
            if (project != null && !string.IsNullOrEmpty(project.FileName))
            {
                return vsSolution.GetGuidOfProjectFile(project.FileName);
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.LogVerbose(Strings.TextBufferIssueTracker_ProjectGuidError, documentFilePath, ex);
        }

        return Guid.Empty;
    }

    private Project GetProject(string filePath)
    {
        threadHandling.ThrowIfNotOnUIThread();
        // Bug #676: https://github.com/SonarSource/sonarlint-visualstudio/issues/676
        // It's possible to have a ProjectItem that doesn't have a ContainingProject
        // e.g. files under the "External Dependencies" project folder in the Solution Explorer
        var projectItem = dte.Solution.FindProjectItem(filePath);
        return projectItem?.ContainingProject;
    }
}
