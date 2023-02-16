/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Newtonsoft.Json;

namespace SonarQube.Client.Helpers
{
    internal class MillisecondUnixTimestampDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override void WriteJson(JsonWriter writer,
            DateTimeOffset value,
            JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override DateTimeOffset ReadJson(JsonReader reader,
            Type objectType,
            DateTimeOffset existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            return reader?.Value is long timestamp 
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp) 
                : default(DateTimeOffset);
        }
    }
}
