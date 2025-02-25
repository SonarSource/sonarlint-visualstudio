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
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.ConnectedMode.Shared;

public interface ISharedBindingConfigFileProvider
{
    SharedBindingConfigModel Read(string filePath);

    bool Exists(string filePath);

    bool Write(string filePath, SharedBindingConfigModel sharedBindingConfigModel);
}

[Export(typeof(ISharedBindingConfigFileProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SharedBindingConfigFileProvider(ILogger logger, IFileSystemService fileSystem) : ISharedBindingConfigFileProvider
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented
    };

    public SharedBindingConfigModel Read(string filePath)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<SharedBindingConfigModel>(fileSystem.File.ReadAllText(filePath));

            if (result.IsSonarCloud())
            {
                var region = CloudServerRegion.GetRegionByName(result.Region);
                result.Region = region.Name;
                result.Uri = region.Url;
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

    public bool Exists(string filePath) =>
        fileSystem.File.Exists(filePath);

    public bool Write(string filePath, SharedBindingConfigModel sharedBindingConfigModel)
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
