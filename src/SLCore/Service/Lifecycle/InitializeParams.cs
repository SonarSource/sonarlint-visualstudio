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

using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.Service.Lifecycle
{
    /// <summary>
    /// SLCore initialization parameters
    /// </summary>
    public record InitializeParams(
        ClientConstantInfoDto clientConstantInfo,
        HttpConfigurationDto httpConfiguration,
        HashSet<BackendCapability> backendCapabilities,
        string storageRoot,
        string workDir,
        List<string> embeddedPluginPaths,
        Dictionary<string, string> connectedModeEmbeddedPluginPathsByKey,
        IReadOnlyCollection<Language> enabledLanguagesInStandaloneMode,
        IReadOnlyCollection<Language> extraEnabledLanguagesInConnectedMode,
        IReadOnlyCollection<string> disabledPluginKeysForAnalysis,
        List<SonarQubeConnectionConfigurationDto> sonarQubeConnections,
        List<SonarCloudConnectionConfigurationDto> sonarCloudConnections,
        string sonarlintUserHome,
        Dictionary<string, StandaloneRuleConfigDto> standaloneRuleConfigByKey,
        bool isFocusOnNewCode,
        TelemetryClientConstantAttributesDto telemetryConstantAttributes,
        TelemetryMigrationDto telemetryMigration,
        LanguageSpecificRequirements languageSpecificRequirements);
}
