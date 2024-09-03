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
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.Persistence;

public interface IJsonFileHandler
{
    /// <summary>
    /// Reads the json file and deserializes its content to the provided type.
    /// </summary>
    /// <typeparam name="T">The type of the model that will be serialized</typeparam>
    /// <param name="filePath">The path to the file</param>
    /// <returns>Returns the content of the json file deserialized to the provided type.</returns>
    T ReadFile<T>(string filePath) where T : class;
    
    /// <summary>
    /// Tries to deserialize the model and write it to the json file.
    /// If the file does not exist, it will be created.
    /// </summary>
    /// <typeparam name="T">The type of the model that will be deserialized</typeparam>
    /// <param name="filePath">The path to the file</param>
    /// <param name="model">The model that will be deserialized</param>
    /// <returns>True if the model was deserialized successfully and written to the file. False otherwise</returns>
    bool TryWriteToFile<T>(string filePath, T model) where T : class;
}

[Export(typeof(IJsonFileHandler))]
[PartCreationPolicy(CreationPolicy.NonShared)]
public class JsonFileHandler : IJsonFileHandler
{
    private readonly ILogger logger;
    private readonly IFileSystem fileSystem;
    private readonly IJsonSerializer jsonSerializer;

    [ImportingConstructor]
    public JsonFileHandler(IJsonSerializer jsonSerializer, ILogger logger) : this(new FileSystem(), jsonSerializer, logger) { }

    internal /* for testing */ JsonFileHandler(IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger)
    {
        this.fileSystem = fileSystem;
        this.jsonSerializer = jsonSerializer;
        this.logger = logger;
    }

    public T ReadFile<T>(string filePath) where T : class
    {
        var jsonContent = fileSystem.File.ReadAllText(filePath);
        return jsonSerializer.Deserialize<T>(jsonContent);
    }

    public bool TryWriteToFile<T>(string filePath, T model) where T : class
    {
        try
        {
            var directoryName = Path.GetDirectoryName(filePath);
            if (!fileSystem.Directory.Exists(directoryName))
            {
                fileSystem.Directory.CreateDirectory(directoryName);
            }

            var wasContentDeserialized = jsonSerializer.TrySerialize(model, out string serializedObj, Formatting.Indented);
            if (wasContentDeserialized)
            {
                fileSystem.File.WriteAllText(filePath, serializedObj);
                return true;
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(ex.Message);
        }

        return false;
    }
}
