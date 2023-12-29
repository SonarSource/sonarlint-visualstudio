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

using System.Collections.Generic;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.Service.Lifecycle
{
    /// <summary>
    /// SLCore initialization parameters
    /// </summary>
    public class InitializeParams
    {
        public ClientConstantsDto clientConstantInfo { get; }
        public FeatureFlagsDto featureFlags { get; }
        public string storageRoot { get; }
        public string workDir { get; }
        public List<string> embeddedPluginPaths { get; }
        public Dictionary<string, string> connectedModeEmbeddedPluginPathsByKey { get; }
        public List<Language> enabledLanguagesInStandaloneMode { get; }
        public List<Language> extraEnabledLanguagesInConnectedMode { get; }
        public List<SonarQubeConnectionConfigurationDto> sonarQubeConnections { get; }
        public List<SonarCloudConnectionConfigurationDto> sonarCloudConnections { get; }
        public string sonarlintUserHome { get; }
        public Dictionary<string, StandaloneRuleConfigDto> standaloneRuleConfigByKey { get; }
        public bool isFocusOnNewCode { get; }
        public TelemetryClientConstantAttributesDto telemetryConstantAttributes { get; }
        public string clientNodeJsPath { get; }

        public InitializeParams(ClientConstantsDto clientConstantInfo,
            FeatureFlagsDto featureFlags,
            string storageRoot,
            string workDir,
            List<string> embeddedPluginPaths,
            Dictionary<string, string> connectedModeEmbeddedPluginPathsByKey,
            List<Language> enabledLanguagesInStandaloneMode,
            List<Language> extraEnabledLanguagesInConnectedMode,
            List<SonarQubeConnectionConfigurationDto> sonarQubeConnections,
            List<SonarCloudConnectionConfigurationDto> sonarCloudConnections,
            string sonarlintUserHome,
            Dictionary<string, StandaloneRuleConfigDto> standaloneRuleConfigByKey,
            bool isFocusOnNewCode,
            TelemetryClientConstantAttributesDto telemetryConstantAttributes,
            string clientNodeJsPath)
        {
            this.clientConstantInfo = clientConstantInfo;
            this.featureFlags = featureFlags;
            this.storageRoot = storageRoot;
            this.workDir = workDir;
            this.embeddedPluginPaths = embeddedPluginPaths;
            this.connectedModeEmbeddedPluginPathsByKey = connectedModeEmbeddedPluginPathsByKey;
            this.enabledLanguagesInStandaloneMode = enabledLanguagesInStandaloneMode;
            this.extraEnabledLanguagesInConnectedMode = extraEnabledLanguagesInConnectedMode;
            this.sonarQubeConnections = sonarQubeConnections;
            this.sonarCloudConnections = sonarCloudConnections;
            this.sonarlintUserHome = sonarlintUserHome;
            this.standaloneRuleConfigByKey = standaloneRuleConfigByKey;
            this.isFocusOnNewCode = isFocusOnNewCode;
            this.telemetryConstantAttributes = telemetryConstantAttributes;
            this.clientNodeJsPath = clientNodeJsPath;
        }
    }
}
