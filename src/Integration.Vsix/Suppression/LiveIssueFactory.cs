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
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
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
    internal sealed class LiveIssueFactory : ILiveIssueFactory
    {
        private readonly Workspace workspace;

        /// <summary>
        /// Mapping from full project file path to the unique project id
        /// </summary>
        private readonly IDictionary<string, string> projectPathToProjectIdMap;

        public LiveIssueFactory(Workspace workspace, IVsSolution vsSolution)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            if (vsSolution == null)
            {
                throw new ArgumentNullException(nameof(vsSolution));
            }

            this.workspace = workspace;
            this.projectPathToProjectIdMap = BuildProjectPathToIdMap(vsSolution);
        }

        /// <summary>
        /// Attempts to fetch the extra information required to map from a Roslyn issue
        /// to a SonarQube server issue. Returns null if there is not enough information
        /// to produce the mapping.
        /// </summary>
        public LiveIssue Create(SyntaxTree syntaxTree, Diagnostic diagnostic)
        {
            if (syntaxTree == null)
            {
                return null;
            }

            var projectFilePath = workspace?.CurrentSolution?.GetDocument(syntaxTree)?.Project?.FilePath;
            if (projectFilePath == null)
            {
                return null;
            }

            string projectGuid;
            if (!projectPathToProjectIdMap.TryGetValue(projectFilePath, out projectGuid))
            {
                Debug.Fail($"Expecting to have a project id for the Roslyn project: {projectFilePath}");
                return null;
            }

            if (diagnostic.Location == Location.None) // Project-level issue
            {
                return new LiveIssue(diagnostic, projectGuid);
            }

            var lineSpan = diagnostic.Location.GetLineSpan();
            var relativeFilePath = FileUtilities.GetRelativePath(projectFilePath, lineSpan.Path);

            var isFileLevelIssue = lineSpan.StartLinePosition.Line == 0 &&
                lineSpan.StartLinePosition.Character == 0 &&
                lineSpan.EndLinePosition.Line == 0 &&
                lineSpan.EndLinePosition.Character == 0;

            if (isFileLevelIssue) // File-level issue
            {
                return new LiveIssue(diagnostic, projectGuid, issueFilePath: relativeFilePath);
            }

            var startLine = lineSpan.StartLinePosition.Line;
            var additionalLineCount = lineSpan.EndLinePosition.Line - startLine;
            var lineText = syntaxTree.GetText().Lines[startLine + additionalLineCount].ToString();
            var sonarQubeLineNumber = lineSpan.StartLinePosition.Line + 1; // Roslyn lines are 0-based, SonarQube lines are 1-based

            return new LiveIssue(diagnostic, projectGuid,
                issueFilePath: relativeFilePath,
                startLine: sonarQubeLineNumber,
                wholeLineText: lineText); // Line-level issue
        }

        internal /* for testing */ static IDictionary<string, string> BuildProjectPathToIdMap(IVsSolution solution)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Call with nulls to get the number of files
            const uint grfGetOpsIncludeUnloadedFiles = 0; // required since the projects might not have finished loading
            uint fileCount;
            var result = solution.GetProjectFilesInSolution(grfGetOpsIncludeUnloadedFiles, 0, null, out fileCount);
            if (ErrorHandler.Failed(result))
            {
                return map;
            }

            // 2. Size array and call again to get the data
            string[] fileNames = new string[fileCount];
            result = solution.GetProjectFilesInSolution(grfGetOpsIncludeUnloadedFiles, fileCount, fileNames, out fileCount);
            if (ErrorHandler.Failed(result))
            {
                return map;
            }

            IVsSolution5 soln5 = (IVsSolution5)solution;

            foreach (string projectFile in fileNames)
            {
                // We need to use the same project id that is used by the Scanner for MSBuild.
                // For non-.Net Core projects, the scanner will use the <ProjectGuid> value in the project.
                // .Net Core projects don't have a <ProjectGuid> property so the scanner uses the GUID allocated
                // to the project in the solution file.
                // Fortunately, this is the logic used by soln5.GetGuidOfProjectFile... so we can just use that.
                Guid projectGuid = soln5.GetGuidOfProjectFile(projectFile);

                // Overwrite duplicate entries. This won't happen for real project files: it will only happen
                // for solution folders with duplicate names, which we don't care about.
                map[projectFile] = projectGuid.ToString().Replace("{", "").Replace("}", "");
            }
            return map;
        }
    }
}