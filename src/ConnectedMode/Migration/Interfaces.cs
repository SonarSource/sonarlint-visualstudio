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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// Handle the non-UI part of migrating to the new unintrusive Connected Mode
    /// file settings
    /// </summary>
    internal interface IConnectedModeMigration
    {
        // TODO: decide if the method should return a list of specific manual cleanup
        // steps that the user still needs to perform
        Task MigrateAsync(IProgress<MigrationProgress> progress, CancellationToken token);
    }

    /// <summary>
    /// Data class containing information about migration progress
    /// </summary>
    internal class MigrationProgress
    {
        public MigrationProgress(int currentProject, int totalProjects, string message, bool isWarning)
        {
            CurrentProject = currentProject;
            TotalProjects = totalProjects;
            Message = message;
            IsWarning = isWarning;
        }

        public int CurrentProject { get; }

        public int TotalProjects { get; }

        public string Message { get; }

        /// <summary>
        /// Indicates whether the step is a warning or not
        /// i.e. true if the migration process didn't manage to carry out part of the cleanup
        /// sucessfully
        /// </summary>
        public bool IsWarning { get; }
    }

    /// <summary>
    /// Finds the set of MSBuild files that need to be cleaned as part of the migration
    /// </summary>
    internal interface IFileProvider
    {
        /// <summary>
        /// Returns a list of full paths to the set of MSBuild files to be cleaned
        /// </summary>
        /// <remarks>The files can be project files or imported props/targets etc</remarks>
        Task<IEnumerable<string>> GetFilesAsync(CancellationToken token);
    }

    /// <summary>
    /// Contract for a component that will clean ruleset and additional file references for a single file
    /// </summary>
    /// <remarks>The file will be an XML file. It could be ruleset file or an MSBuild project file (including
    /// .props, .targets and Directory.Build.props/targets</remarks>
    internal interface IFileCleaner
    {
        /// <summary>
        /// Removes unwanted settings from the supplied XML content
        /// </summary>
        /// <returns>The modified content, or null if the text was not modified</returns>
        Task<string> CleanAsync(string content, LegacySettings legacySettings, CancellationToken token);
    }

    /// <summary>
    /// Data class containing information about the paths to the legacy generated files
    /// </summary>
    /// <remarks>Required by <see cref="IFileCleaner"/> to identify the references to remove</remarks>
    internal class LegacySettings
    {
        public LegacySettings(
            string sonarLintFolderPath,
            string partialCSharpRuleSetPath,
            string partialCSharpSonarLintXmlPath,
            string partialVBRuleSetPath,
            string partialVBSonarLintXmlPath)
        {
            LegacySonarLintFolderPath = sonarLintFolderPath ?? throw new ArgumentNullException(nameof(sonarLintFolderPath));
            PartialCSharpRuleSetPath = partialCSharpRuleSetPath ?? throw new ArgumentNullException(nameof(partialCSharpRuleSetPath));
            PartialCSharpSonarLintXmlPath = partialCSharpSonarLintXmlPath ?? throw new ArgumentNullException(nameof(partialCSharpSonarLintXmlPath));
            PartialVBRuleSetPath = partialVBRuleSetPath ?? throw new ArgumentNullException(nameof(partialVBRuleSetPath));
            PartialVBSonarLintXmlPath = partialVBSonarLintXmlPath ?? throw new ArgumentNullException(nameof(partialVBSonarLintXmlPath));
        }

        /// <summary>
        /// Full path to the legacy .sonarlint folder path
        /// </summary>
        public string LegacySonarLintFolderPath { get; }

        /// <summary>
        /// Partial path to the generated ruleset e.g. ".sonarlint\slvs_samples_bound_vs2019csharp.ruleset"
        /// </summary>
        public string PartialCSharpRuleSetPath { get; }

        /// <summary>
        /// Partial path to the generated SonarLint.xml file e.g. ".sonarlint\slvs_samples_bound_vs2019\CSharp\SonarLint.xml"
        /// </summary>
        public string PartialCSharpSonarLintXmlPath { get; }

        /// <summary>
        /// Partial path to the generated ruleset e.g. ".sonarlint\slvs_samples_bound_vs2019vb.ruleset"
        /// </summary>
        public string PartialVBRuleSetPath { get; }

        /// <summary>
        /// Partial path to the generated SonarLint.xml file e.g. ".sonarlint\slvs_samples_bound_vs2019\VB\SonarLint.xml"
        /// </summary>
        public string PartialVBSonarLintXmlPath { get; }
    }

    /// <summary>
    /// Abstraction over file operations required during migration against files that might
    /// be in a VS solution.
    /// </summary>
    /// <remarks>
    /// Types involved in migration should use this abstraction for all reads/writes to files
    /// that might be part of the solution.
    /// It gives us a way to centralise any additional API calls that might be needed
    /// e.g. calling VS source-control APIs notifying it that files have changed or been deleted.
    /// </remarks>
    internal interface IVsAwareFileSystem
    {
        /// <summary>
        /// Returns the contents of the specified document as text
        /// </summary>
        /// <returns>The data might be fetched from disc or from in-memory, depending on
        /// whether the file is open in VS</returns>
        Task<string> LoadAsTextAsync(string filePath);

        /// <summary>
        /// Writes the text back to the document
        /// </summary>
        /// <remarks>The data might be persisted to disc, or used to update the in-memory representation
        /// if the document is open in VS</remarks>
        Task SaveAsync(string filePath, string text);

        /// <summary>
        /// Deletes the folder from disc
        /// </summary>
        Task DeleteFolderAsync(string folderPath);
    }
}
