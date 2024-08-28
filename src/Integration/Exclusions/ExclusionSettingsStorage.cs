/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;
using static System.String;

namespace SonarLint.VisualStudio.Integration.Exclusions
{
    [Export(typeof(IExclusionSettingsStorage))]
    internal class ExclusionSettingsStorage : IExclusionSettingsStorage
    {
        private readonly IConfigurationProvider bindingConfigProvider;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public ExclusionSettingsStorage(IConfigurationProvider bindingConfigProvider, ILogger logger)
            : this(bindingConfigProvider, logger, new FileSystem())
        {
        }

        internal ExclusionSettingsStorage(IConfigurationProvider bindingConfigProvider, 
            ILogger logger,
            IFileSystem fileSystem)
        {
            this.bindingConfigProvider = bindingConfigProvider;
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public void SaveSettings(ServerExclusions settings)
        {
            var bindingConfiguration = bindingConfigProvider.GetConfiguration();

            if (bindingConfiguration.Mode == SonarLintMode.Standalone)
            {
                Debug.Fail("Not expecting to be called in Standalone mode");
                throw new InvalidOperationException("Cannot save exclusions in Standalone mode.");
            }

            var fileContent = JsonConvert.SerializeObject(settings, Formatting.Indented);
            var exclusionsFilePath = GetFilePath(bindingConfiguration);

            fileSystem.File.WriteAllText(exclusionsFilePath, fileContent);
        }

        public ServerExclusions GetSettings()
        {
            try
            {
                var bindingConfiguration = bindingConfigProvider.GetConfiguration();

                if (bindingConfiguration.Mode == SonarLintMode.Standalone)
                {
                    logger.LogVerbose("[ExclusionSettingsStorage] Standalone mode, exclusions are not supported.");
                    return null;
                }

                var exclusionsFilePath = GetFilePath(bindingConfiguration);

                if (!fileSystem.File.Exists(exclusionsFilePath))
                {
                    logger.WriteLine(Strings.ExclusionGetError, Format(Strings.ExclusionFileNotFound, exclusionsFilePath));
                    return null;
                }

                var fileContent = fileSystem.File.ReadAllText(exclusionsFilePath);

                logger.LogVerbose("[ExclusionSettingsStorage] File content: {0}", fileContent);

                return JsonConvert.DeserializeObject<ServerExclusions>(fileContent);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose("[ExclusionSettingsStorage] GetSettings error: {0}", ex.ToString());
                logger.WriteLine(Strings.ExclusionGetError, ex.Message);
            }

            return null;
        }

        private static string GetFilePath(BindingConfiguration bindingConfiguration) => 
            Path.Combine(bindingConfiguration.BindingConfigDirectory, "sonar.settings.json");
    }
}
