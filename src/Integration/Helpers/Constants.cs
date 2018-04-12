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

namespace SonarLint.VisualStudio.Integration
{
    public static class Constants
    {
        /// <summary>
        /// The property key which corresponds to the code analysis rule set
        /// </summary>
        public const string CodeAnalysisRuleSetPropertyKey = "CodeAnalysisRuleSet";

        /// <summary>
        /// The property key which corresponds to the code analysis rule set directories
        /// </summary>
        public const string CodeAnalysisRuleSetDirectoriesPropertyKey = "CodeAnalysisRuleSetDirectories";

        /// <summary>
        /// The directory name of the SonarQube specific files that are being created in legacy connected mode
        /// </summary>
        public const string LegacySonarQubeManagedFolderName = "SonarQube";

        /// <summary>
        /// The directory name of the SonarQube specific files that are being created in legacy connected mode
        /// </summary>
        public const string SonarlintManagedFolderName = ".sonarlint";

        /// <summary>
        /// The generated rule set name
        /// </summary>
        public const string RuleSetName = "SonarQube";

        /// <summary>
        /// The property key which corresponds to the Roslyn analyzer additional files
        /// </summary>
        public const string AdditionalFilesItemTypeName = "AdditionalFiles";

        /// <summary>
        /// The SonarQube home page
        /// </summary>
        public const string SonarQubeHomeWebUrl = "http://sonarqube.org";

        /// <summary>
        /// SonarLint issues home page
        /// </summary>
        public const string SonarLintIssuesWebUrl = "https://groups.google.com/forum/#!forum/sonarlint";

        /// <summary>
        /// The property key which corresponds to the ItemType of a <see cref="EnvDTE.ProjectItem"/>.
        /// </summary>
        public const string ItemTypePropertyKey = "ItemType";

        /// <summary>
        /// Ruleset file extension
        /// </summary>
        public const string RuleSetFileExtension = "ruleset";

        /// <summary>
        /// The build property key which corresponds to the explicit SonarQube project exclusion.
        /// </summary>
        public const string SonarQubeExcludeBuildPropertyKey = "SonarQubeExclude";

        /// <summary>
        /// The build property key which corresponds to the explicit SonarQube test project identification.
        /// </summary>
        public const string SonarQubeTestProjectBuildPropertyKey = "SonarQubeTestProject";

    }
}
