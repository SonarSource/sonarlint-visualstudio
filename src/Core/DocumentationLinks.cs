﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Contains all documentation links used in-product.
    /// </summary>
    public static class DocumentationLinks
    {
        public const string HomePage = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/";
        public const string LanguageSpecificRequirements = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/getting-started/requirements/#language-specific-requirements";
        public const string LanguageSpecificRequirements_JsTs = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/getting-started/requirements/#nodejs-prerequisites-for-js-and-ts";
        public const string MigrateToConnectedModeV7 = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/migrate-connected-mode-to-v7/";
        public const string MigrateToConnectedModeV7_NotesForTfvcUsers
            = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/migrate-connected-mode-to-v7/#notes-for-tfvc-users";
        public const string ConnectedMode = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/connected-mode/";
        public const string ConnectedModeBenefits = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/connected-mode#benefits";
        public const string TaintVulnerabilities = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/using/taint-vulnerabilities/";
        public const string DisablingARule = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/using/rules/#disabling-a-rule";
        public const string SettingsJsonFile = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/using/rules/#settingsjson-file-format-and-location";
        public const string FileExclusionsPatternJsonFile = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/using/file-exclusions/#using-wildcards";
        public const string UseSharedBinding = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/connected-mode-setup/#bind-using-shared-configuration";
        public const string SetupSharedBinding = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/connected-mode-setup/#save-the-connection-binding";
        public const string CleanCode = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/using/software-qualities";
        public const string OpenInIdeIssueLocation = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/troubleshooting/#no-matching-issue-found";
        public const string OpenInIdeBindingSetup = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/troubleshooting/#no-matching-project-found";
        public const string UnbindingProject = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/connected-mode-setup/#unbinding-a-project";
        public const string SslCertificate = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/team-features/advanced-configuration/#server-ssl-certificates";
        public const string AnalysisProperties = "https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/using/scan-my-project/#specify-additional-analyzer-properties";

        public static readonly Uri UnbindingProjectUri = new(UnbindingProject);
        public static readonly Uri ConnectedModeUri = new(ConnectedMode);
        public static readonly Uri ConnectedModeBenefitsUri = new(ConnectedModeBenefits);
        public static readonly Uri SettingsJsonFileUri = new(SettingsJsonFile);
        public static readonly Uri FileExclusionsPatternUri = new(FileExclusionsPatternJsonFile);
        public static readonly Uri DisablingARuleUri = new(DisablingARule);
        public static readonly Uri AnalysisPropertiesUri = new(AnalysisProperties);
    }
}
