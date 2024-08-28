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
    bool TryReadFile<T>(string filePath, out T content) where T : class;
    bool TryWriteToFile<T>(string filePath, T model) where T : class;
}

[Export(typeof(IJsonFileHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class JsonFileHandler : IJsonFileHandler
{
    private readonly ILogger logger;
    private readonly IFileSystem fileSystem;
    private readonly IJsonSerializer jsonSerializer;
    private static readonly object Locker = new();

    [ImportingConstructor]
    public JsonFileHandler(IJsonSerializer jsonSerializer, ILogger logger) : this(new FileSystem(), jsonSerializer, logger) { }

    internal /* for testing */ JsonFileHandler(IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger)
    {
        this.fileSystem = fileSystem;
        this.jsonSerializer = jsonSerializer;
        this.logger = logger;
    }

    public bool TryReadFile<T>(string filePath, out T content) where T: class
    {
        content = null;
        if (!fileSystem.File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var jsonContent = fileSystem.File.ReadAllText(filePath);
            var wasContentDeserialized = jsonSerializer.TryDeserialize(jsonContent, out T deserializedObj);
            content = deserializedObj;
            return wasContentDeserialized;
          
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(ex.Message);
            return false;
        }
    }

    public bool TryWriteToFile<T>(string filePath, T model) where T : class
    {
        lock (Locker)
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
}
