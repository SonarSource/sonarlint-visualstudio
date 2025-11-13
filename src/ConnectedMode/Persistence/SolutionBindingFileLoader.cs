/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

internal class SolutionBindingFileLoader : ISolutionBindingFileLoader
{
    private readonly IFileSystem fileSystem;
    private readonly ILogger logger;

    public SolutionBindingFileLoader(ILogger logger)
        : this(logger, new FileSystem())
    {
    }

    internal SolutionBindingFileLoader(ILogger logger, IFileSystem fileSystem)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public bool Save(string filePath, BindingJsonModel project)
    {
        var serializedProject = Serialize(project);

        return SafePerformFileSystemOperation(() => WriteConfig(filePath, serializedProject));
    }

    public bool DeleteBindingDirectory(string configFilePath)
    {
        if (fileSystem.File.Exists(configFilePath))
        {
            return SafePerformFileSystemOperation(() =>
            {
                var directoryName = Path.GetDirectoryName(configFilePath);
                fileSystem.Directory.Delete(directoryName, true);
            });
        }

        logger.LogVerbose(PersistenceStrings.BindingDirectoryNotDeleted, configFilePath);
        return false;
    }

    public BindingJsonModel Load(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !fileSystem.File.Exists(filePath))
        {
            return null;
        }

        string configJson = null;

        if (SafePerformFileSystemOperation(() => ReadConfig(filePath, out configJson)))
        {
            try
            {
                return Deserialize(configJson);
            }
            catch (JsonException)
            {
                logger.WriteLine(PersistenceStrings.FailedToDeserializeSQCOnfiguration, filePath);
            }
        }

        return null;
    }

    private void WriteConfig(string configFile, string serializedProject)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(configFile));

        var directoryName = Path.GetDirectoryName(configFile);

        if (!fileSystem.Directory.Exists(directoryName))
        {
            fileSystem.Directory.CreateDirectory(directoryName);
        }

        fileSystem.File.WriteAllText(configFile, serializedProject);
    }

    private void ReadConfig(string configFile, out string text) => text = fileSystem.File.ReadAllText(configFile);

    private bool SafePerformFileSystemOperation(Action operation)
    {
        Debug.Assert(operation != null);

        try
        {
            operation();
            return true;
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.WriteLine(e.Message);
            return false;
        }
    }

    private BindingJsonModel Deserialize(string projectJson) =>
        JsonConvert.DeserializeObject<BindingJsonModel>(projectJson,
            new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat, DateTimeZoneHandling = DateTimeZoneHandling.Local, DateParseHandling = DateParseHandling.DateTimeOffset
            });

    private string Serialize(BindingJsonModel project) => JsonConvert.SerializeObject(project, Formatting.Indented, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });
}
