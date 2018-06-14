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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;

namespace SonarQube.Client.Helpers
{
    public static class QueryStringSerializer
    {
        private const string QueryDelimiter = "&";

        public static string ToQueryString(object request)
        {
            if (request == null)
            {
                return null;
            }

            return string.Join(QueryDelimiter, JObject.FromObject(request)
                .Properties()
                .SelectMany(SerializeProperty));
        }

        private static IEnumerable<string> SerializeProperty(JProperty property)
        {
            return GetValues(property.Value)
                .Select(SerializeValue)
                .Select(HttpUtility.UrlEncode)
                .Select(value => $"{property.Name}={value}");
        }

        private static IEnumerable<JValue> GetValues(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                throw new NotSupportedException("Nested objects cannot be serialized in query string format.");
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var value in ((JArray)token).OfType<JValue>())
                {
                    yield return value;
                }
            }
            else if (token.Type != JTokenType.Null)
            {
                yield return token as JValue;
            }
        }

        private static string SerializeValue(JValue value)
        {
            if (value.Type == JTokenType.Date)
            {
                // ISO 8601
                return value.ToString("o", CultureInfo.InvariantCulture);
            }
            if (value.Type == JTokenType.Boolean)
            {
                return value.ToString().ToLowerInvariant();
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
