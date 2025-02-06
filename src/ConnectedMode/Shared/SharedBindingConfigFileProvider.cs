/*
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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Shared
{
    public interface ISharedBindingConfigFileProvider
    {
        SharedBindingConfigModel ReadSharedBindingConfigFile(string filePath);

        bool WriteSharedBindingConfigFile(string filePath, SharedBindingConfigModel sharedBindingConfigModel);
    }

    [Export(typeof(ISharedBindingConfigFileProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SharedBindingConfigFileProvider : ISharedBindingConfigFileProvider
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public SharedBindingConfigFileProvider(ILogger logger) : this(logger, new FileSystem())
        { }

        internal SharedBindingConfigFileProvider(ILogger logger, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public SharedBindingConfigModel ReadSharedBindingConfigFile(string filePath)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<SharedBindingConfigModel>(fileSystem.File.ReadAllText(filePath));

                if (result.IsSonarCloud())
                {
                    result.Region = string.IsNullOrEmpty(result.Region) ? CloudServerRegion.Eu.Name : result.Region;
                    result.Uri = CloudServerRegion.GetRegionByName(result.Region).Url;
                }

                if (!string.IsNullOrWhiteSpace(result.ProjectKey)
                    && result.Uri != null
                    && (result.Uri.Scheme == Uri.UriSchemeHttp || result.Uri.Scheme == Uri.UriSchemeHttps))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"[SharedBindingConfigFileProvider] Unable to read shared config file: {ex.Message}");
            }

            return null;
        }

        public bool WriteSharedBindingConfigFile(string filePath, SharedBindingConfigModel sharedBindingConfigModel)
        {
            bool result = false;
            try
            {
                if (sharedBindingConfigModel.IsSonarCloud())
                {
                    sharedBindingConfigModel.Uri = null;
                    sharedBindingConfigModel.Region ??= CloudServerRegion.Eu.Name;
                }

                var fileContent = JsonConvert.SerializeObject(sharedBindingConfigModel, SerializerSettings);

                var sharedDirectory = Path.GetDirectoryName(filePath);
                if (!fileSystem.Directory.Exists(sharedDirectory))
                {
                    fileSystem.Directory.CreateDirectory(sharedDirectory);
                }
                fileSystem.File.WriteAllText(filePath, fileContent);
                result = true;
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"[SharedBindingConfigFileProvider] Unable to write shared config file: {ex.Message}");
            }
            return result;
        }
    }
}
