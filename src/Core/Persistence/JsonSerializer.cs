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
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.Persistence;

public interface IJsonSerializer
{
    bool TryDeserialize<T>(string json, out T deserializedObj, JsonSerializerSettings serializerSettings = null) where T : class;
    T Deserialize<T>(string json, JsonSerializerSettings serializerSettings = null) where T : class;
    bool TrySerialize<T>(T objectToSerialize, out string serializedObj, Formatting formatting = Formatting.None,
        JsonSerializerSettings serializerSettings = null) where T : class;
}

[Export(typeof(IJsonSerializer))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class JsonSerializer : IJsonSerializer
{
    private readonly ILogger logger;
    private readonly Func<object, Formatting, JsonSerializerSettings, string> serializeFunc;

    [ImportingConstructor]
    public JsonSerializer(ILogger logger) : this(logger, JsonConvert.SerializeObject)
    {
        this.logger = logger;
    }

    internal /* for testing */ JsonSerializer(ILogger logger, Func<object, Formatting, JsonSerializerSettings, string> serializeFunc)
    {
        this.logger = logger;
        this.serializeFunc = serializeFunc;
    }

    public bool TryDeserialize<T>(string json, out T deserializedObj, JsonSerializerSettings serializerSettings = null) where T : class
    {
        deserializedObj = null;
        try
        {
            deserializedObj = Deserialize<T>(json, serializerSettings);
            return true;
        }
        catch (Exception)
        {
            logger.WriteLine(string.Format(PersistenceStrings.FailedToDeserializeObject, typeof(T).Name));
            return false;
        }
    }

    public T Deserialize<T>(string json, JsonSerializerSettings serializerSettings = null) where T : class
    {
       return JsonConvert.DeserializeObject<T>(json, serializerSettings);
    }

    public bool TrySerialize<T>(T objectToSerialize, out string serializedObj, Formatting formatting = Formatting.None, JsonSerializerSettings serializerSettings = null) where T: class
    {
        serializedObj = null;
        try
        {
            serializedObj = serializeFunc(objectToSerialize, formatting, serializerSettings);
            return true;
        }
        catch (Exception)
        {
            logger.WriteLine(string.Format(PersistenceStrings.FailedToSerializeObject, typeof(T).Name));
            return false;
        }
    }
}
