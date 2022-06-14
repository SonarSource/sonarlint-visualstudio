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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Exclusions
{
    [Export(typeof(IExclusionSettingsFileStorage))]
    internal class ExclusionSettingsFileStorage : IExclusionSettingsFileStorage
    {
        private readonly IConfigurationProviderService configurationProviderService;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public ExclusionSettingsFileStorage(ILogger logger, IFileSystem fileSystem, IConfigurationProviderService configurationProviderService)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;         
            this.configurationProviderService = configurationProviderService;            
        }

        public ServerExclusions GetSettings()
        {
            try
            {
                var bindingConfiguration = configurationProviderService.GetConfiguration();

                if (bindingConfiguration.Mode == SonarLintMode.Standalone)
                {
                    logger.WriteLine(Strings.ExclusionOnStandaloneNotSupported);
                    return null;
                }

                if (!fileSystem.File.Exists(GetFilePath(bindingConfiguration.BindingConfigDirectory)))
                {
                    logger.WriteLine(String.Format(Strings.ExclusionGetError, Strings.ExclusionFileNotFound));
                    return null;
                }

                var fileContent = fileSystem.File.ReadAllText(GetFilePath(bindingConfiguration.BindingConfigDirectory));
                return JsonConvert.DeserializeObject<ServerExclusions>(fileContent);
            }
            catch(Exception ex)
            {
                logger.WriteLine(String.Format(Strings.ExclusionGetError, ex.Message));
            }

            return null;
        }

        // File directory added because configurationProviderService cannot be used reliably during the save in first binding.
        public void SaveSettings(ServerExclusions settings, string fileDirectory)
        {
            try
            {                
                var fileContent = JsonConvert.SerializeObject(settings);
                
                fileSystem.File.WriteAllText(GetFilePath(fileDirectory), fileContent);
            } catch (Exception ex)
            {
                logger.WriteLine(String.Format(Strings.ExclusionSaveError, ex.Message));
            }
           
        }

        private string GetFilePath(string fileDirectory)
        {
            return Path.Combine(fileDirectory, "sonar.settings.json");            
        }
    }
}
