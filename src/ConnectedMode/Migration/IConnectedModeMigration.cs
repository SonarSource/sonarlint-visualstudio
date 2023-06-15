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
    /// Contract for a component that will clean ruleset and additional file references for a single filee
    /// </summary>
    /// <remarks>The file will be an MSBuild file i.e. XML. It could be project file/imported prop or targets/
    /// Directory.Build.props/targets</remarks>
    internal interface IFileCleaner
    {
        Task CleanAsync(string filePath, LegacySettings legacySettings, CancellationToken token);
    }

    /// <summary>
    /// Data class containing information about the paths to the legacy generated files
    /// </summary>
    /// <remarks>Required by <see cref="IFileCleaner"/> to identify the references to remove</remarks>
    internal class LegacySettings
    {
        public LegacySettings(string partialRuleSetPath, string partialSonarLintXmlPath)
        {
            PartialRuleSetPath = partialRuleSetPath ?? throw new ArgumentNullException(nameof(partialRuleSetPath));
            PartialSonarLintXmlPath = partialSonarLintXmlPath ?? throw new ArgumentNullException(nameof(partialSonarLintXmlPath));
        }

        /// <summary>
        /// Partial path to the generated ruleset e.g. ".sonarlint\slvs_samples_bound_vs2019csharp.ruleset"
        /// </summary>
        public string PartialRuleSetPath { get; }

        /// <summary>
        /// Partial path to the generated SonarLint.xml file e.g. ".sonarlint\slvs_samples_bound_vs2019\CSharp\SonarLint.xml"
        /// </summary>
        public string PartialSonarLintXmlPath { get; }
    }
}
