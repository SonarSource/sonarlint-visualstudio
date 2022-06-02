/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Exclusions;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Exclusions
{
    internal class ExclusionSettingsFileStorage : IExclusionSettingsFileStorage
    {
        private readonly BindingConfiguration bindingConfiguration;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private readonly string filePath;

        public ExclusionSettingsFileStorage(ILogger logger, IFileSystem fileSystem, IConfigurationProviderService configurationProviderService)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
            
            bindingConfiguration = configurationProviderService.GetConfiguration();
            filePath = Path.Combine(bindingConfiguration.BindingConfigDirectory, "sonar.settings.json");
        }

        public ServerExclusions GetSettings()
        {
            try
            {
                if(bindingConfiguration.Mode == SonarLintMode.Standalone)
                {
                    logger.WriteLine(Strings.ExclusionOnStandaloneNotSupported);
                    return null;
                }

                if (!fileSystem.File.Exists(filePath))
                {
                    logger.WriteLine(String.Format(Strings.ExclusionGetError, Strings.ExclusionFileNotFound));
                    return null;
                }

                var fileContent = fileSystem.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<ServerExclusions>(fileContent);
            }
            catch(Exception ex)
            {
                logger.WriteLine(String.Format(Strings.ExclusionGetError, ex.Message));
            }

            return null;
        }

        public void SaveSettings(ServerExclusions settings)
        {
            try
            {
                if (bindingConfiguration.Mode == SonarLintMode.Standalone)
                {
                    logger.WriteLine(Strings.ExclusionOnStandaloneNotSupported);
                    return;
                }
                var fileContent = JsonConvert.SerializeObject(settings);
                
                fileSystem.File.WriteAllText(filePath, fileContent);
            } catch (Exception ex)
            {
                logger.WriteLine(String.Format(Strings.ExclusionSaveError, ex.Message));
            }
           
        }
    }
}
