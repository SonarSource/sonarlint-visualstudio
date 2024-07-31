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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Xml.Serialization;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry.Legacy;

namespace SonarLint.VisualStudio.Integration.Telemetry.Legacy;

[Export(typeof(ITelemetryDataRepository))]
[PartCreationPolicy(CreationPolicy.Shared)]
public sealed class TelemetryDataRepository : ITelemetryDataRepository
{
    private readonly string storageFilePath;
    private readonly XmlSerializer telemetrySerializer = new(typeof(TelemetryData));
    private readonly IFileSystem fileSystem;

    public TelemetryDataRepository() : this(new FileSystem(), EnvironmentVariableProvider.Instance)
    {
    }

    internal /* for testing */ TelemetryDataRepository(IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariables)
    {
        this.fileSystem = fileSystem;
        storageFilePath = GetStorageFilePath(environmentVariables);
    }

    public TelemetryData ReadTelemetryData()
    {
        TelemetryData data = null;
        RetryHelper.RetryOnException(3, TimeSpan.FromSeconds(2), () => { data = ReadXmlFile(); });
        return data;
    }

    private TelemetryData ReadXmlFile()
    {
        if (!fileSystem.File.Exists(storageFilePath))
        {
            return null;
        }
        
        try
        {
            var fileContent = fileSystem.File.ReadAllText(storageFilePath);
            return telemetrySerializer.Deserialize(new StringReader(fileContent)) as TelemetryData;
        }
        catch (InvalidOperationException)
        {
            fileSystem.File.Delete(storageFilePath);
        }

        return null;
    }

    internal static string GetStorageFilePath(IEnvironmentVariableProvider environmentVariables)
    {
        // Note: the data is stored in the roaming profile, so it will be sync across machines for domain-joined users.
        var appDataFolder = environmentVariables.GetSLVSAppDataRootPath();
        var filePath = Path.Combine(appDataFolder, "telemetry.xml");
        return Path.GetFullPath(filePath); // get rid of the .. in file path
    }
}
