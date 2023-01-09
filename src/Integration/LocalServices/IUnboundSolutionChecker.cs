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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IUnboundSolutionChecker
    {
        /// <summary>
        /// Returns true/false if the currently opened solution/folder is bound and requires re-binding
        /// </summary>
        Task<bool> IsBindingUpdateRequired(CancellationToken token);
    }

    internal class UnboundSolutionChecker : IUnboundSolutionChecker
    {
        private readonly IExclusionSettingsStorage exclusionSettingsStorage;
        private readonly IConfigurationProvider bindingConfigProvider;
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;

        public UnboundSolutionChecker(IExclusionSettingsStorage exclusionSettingsStorage,
            IConfigurationProvider bindingConfigProvider,
            ISonarQubeService sonarQubeService,
            ILogger logger)
        {
            this.exclusionSettingsStorage = exclusionSettingsStorage;
            this.bindingConfigProvider = bindingConfigProvider;
            this.sonarQubeService = sonarQubeService;
            this.logger = logger;
        }

        public async Task<bool> IsBindingUpdateRequired(CancellationToken token)
        {
            try
            {
                var savedExclusions = exclusionSettingsStorage.GetSettings();

                if (savedExclusions == null)
                {
                    return true;
                }

                var bindingConfiguration = bindingConfigProvider.GetConfiguration();

                Debug.Assert(bindingConfiguration.Mode != SonarLintMode.Standalone, "Not expecting to be called in standalone mode.");

                var serverExclusions = await sonarQubeService.GetServerExclusions(bindingConfiguration.Project.ProjectKey, token);

                return !savedExclusions.Equals(serverExclusions);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose("[UnboundSolutionChecker] Failed to check for settings: {0}", ex.ToString());
                logger.WriteLine(Strings.BindingUpdateFailedToCheckSettings, ex.Message);

                return false;
            }
        }
    }
}
