/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Globalization;
using Newtonsoft.Json;

namespace SonarQube.Client.Helpers
{
    public class JavaDateConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(DateTimeOffset) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(ToJavaTimeFormat((DateTimeOffset)value));
        }

        private static string ToJavaTimeFormat(DateTimeOffset date)
        {
            // This is the only format the notifications API accepts. ISO 8601 formats don't work.
            var dateTime = date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            var timezone = date.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", "");

            // The Java format is "yyyy-MM-dd'T'HH:mm:ssZ"
            // For example 2013-05-01T13:00:00+0100
            return dateTime + timezone;
        }
    }
}
