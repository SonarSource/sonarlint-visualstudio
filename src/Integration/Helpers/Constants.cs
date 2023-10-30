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

using SonarLint.VisualStudio.Core;

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
        /// The generated rule set name
        /// </summary>
        public const string RuleSetName = "SonarQube";

        /// <summary>
        /// The property key which corresponds to the Roslyn analyzer additional files
        /// </summary>
        public const string AdditionalFilesItemTypeName = "AdditionalFiles";

        /// <summary>
        /// The documentation page for Connected Mode
        /// </summary>
        public const string ConnectedModeHelpPage = DocumentationLinks.ConnectedMode;

        /// <summary>
        /// SonarLint issues home page
        /// </summary>
        /// <remarks>The link launches the community site filtered to the SonarLint fault page</remarks>
        public const string SonarLintIssuesWebUrl = "https://community.sonarsource.com/tags/c/bug/fault/6/sonarlint";

        /// <summary>
        /// The property key which corresponds to the ItemType of a <see cref="EnvDTE.ProjectItem"/>.
        /// </summary>
        public const string ItemTypePropertyKey = "ItemType";

        public const string FullPathPropertyKey = "FullPath";

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
