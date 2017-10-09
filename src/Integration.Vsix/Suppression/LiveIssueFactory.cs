/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

/* To map from a diagnostic to a SonarQube issue we need to work out the SQ moduleId
 * corresponding to the MSBuild project.
 *
 * To speed things up, we cache the mapping from project file path to project id
 * for the loaded projects (which means we need to rebuild the mapping when the
 * solution changes).
 *
 * Note: this class does not monitor changes to the solution. If the solution/binding
 * changes then create a new instance.
 */

namespace SonarLint.VisualStudio.Integration.Vsix.Suppression
{
    /// <summary>
    /// Factory for <see cref="LiveIssue"/>s i.e. diagnostics that are decorated with
    /// additional information required to map to issues in the SonarQube server format
    /// </summary>
    internal sealed class LiveIssueFactory
    {
        private readonly Workspace workspace;
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Mapping from full project file path to the unique project id
        /// </summary>
        private Dictionary<string, string> projectPathToProjectIdMap;

        public LiveIssueFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            workspace = componentModel.GetService<VisualStudioWorkspace>();

            BuildProjectPathToIdMap();
        }

        /// <summary>
        /// Attempts to fetch the extra information required to map from a Roslyn issue
        /// to a SonarQube server issue. Returns null if there is not enough information
        /// to produce the mapping.
        /// </summary>
        public LiveIssue Create(Diagnostic diagnostic)
        {
            // Get the file and project containing the issue
            SyntaxTree tree = diagnostic.Location?.SourceTree;
            if (tree == null)
            {
                return null;
            }

            if (diagnostic.Location == Location.None)
            {
                return null;
            }

            Project project = workspace?.CurrentSolution?.GetDocument(tree)?.Project;
            if (project == null)
            {
                return null;
            }

            string projectId;
            if (!projectPathToProjectIdMap.TryGetValue(project.FilePath, out projectId))
            {
                Debug.Fail($"Expecting to have a project id for the Roslyn project: {project.FilePath}");
                return null;
            }

            FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();

            int startLine = lineSpan.StartLinePosition.Line;
            int additionalLineCount = lineSpan.EndLinePosition.Line - startLine;
            string lineText = tree.GetText().Lines[startLine + additionalLineCount].ToString();
            string relativeFilePath = FileUtilities.GetRelativePath(project.FilePath, lineSpan.Path);
            int sonarQubeLineNumber = ToSonarQubeLineNumber(lineSpan);

            return new LiveIssue(diagnostic, relativeFilePath, projectId, sonarQubeLineNumber, lineText);
        }

        private void BuildProjectPathToIdMap()
        {
            projectPathToProjectIdMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            IVsSolution solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            // 1. Call with nulls to get the number of files
            const uint grfGetOpsIncludeUnloadedFiles = 0; // required since the projects might not have finished loading
            uint fileCount;
            var result = solution.GetProjectFilesInSolution(grfGetOpsIncludeUnloadedFiles, 0, null, out fileCount);
            if (ErrorHandler.Failed(result))
            {
                return;
            }

            // 2. Size array and call again to get the data
            string[] fileNames = new string[fileCount];
            result = solution.GetProjectFilesInSolution(grfGetOpsIncludeUnloadedFiles, fileCount, fileNames, out fileCount);
            if (ErrorHandler.Failed(result))
            {
                return;
            }

            IVsSolution5 soln5 = (IVsSolution5)solution;

            foreach (string projectFile in fileNames)
            {
                // We need to use the same project id that is used by the Scanner for MSBuild.
                // For non-.Net Core projects, the scanner will use the <ProjectGuid> value in the project.
                // .Net Core projects don't have a <ProjectGuid> property so the scanner uses the GUID allocated
                // to the project in the solution file.
                // Fortunately, this is the logic used by soln5.GetGuidOfProjectFile... so we can just use that.
                Guid projectId = soln5.GetGuidOfProjectFile(projectFile);
                Debug.Assert(projectId != null, "Not expecting VS to return a null project guid");

                projectPathToProjectIdMap.Add(projectFile, projectId.ToString().Replace("{", "").Replace("}", ""));
            }
        }

        private int ToSonarQubeLineNumber(FileLinePositionSpan lineSpan)
        {
            var isFileLevel = lineSpan.StartLinePosition.Line == 0 &&
                lineSpan.StartLinePosition.Character == 0 &&
                lineSpan.EndLinePosition.Line == 0 &&
                lineSpan.EndLinePosition.Character == 0;

            if (isFileLevel)
            {
                return 0; // This is a file-level issue
            }

            // Roslyn lines are 0-based, SonarQube lines are 1-based
            return lineSpan.StartLinePosition.Line + 1;
        }
    }
}