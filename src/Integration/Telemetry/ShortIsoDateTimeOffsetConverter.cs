﻿/*
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

namespace SonarLint.VisualStudio.Integration.Telemetry
{
    /// <summary>
    /// Converts DateTimeOffset to roundtrip (ISO 8601) but with just milliseconds, no ticks
    /// </summary>
    internal class ShortIsoDateTimeOffsetConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            typeof(DateTimeOffset).Equals(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            reader.ReadAsDateTimeOffset();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Use the standard format string for ISO 8601
            var iso8601Date = ((DateTimeOffset)value).ToString("o");

            // Remove the ticks part (not expected by our server side)
            const int millisecondLastIndex = 23;
            const int timeOffsetFirstIndex = 27;
            var simplifiedDate = iso8601Date.Substring(0, millisecondLastIndex) + iso8601Date.Substring(timeOffsetFirstIndex);

            writer.WriteValue(simplifiedDate);
        }
    }
}
