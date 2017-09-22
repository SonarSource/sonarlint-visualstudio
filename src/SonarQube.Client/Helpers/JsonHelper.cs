/*
 * SonarQube Client
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Text;
using Newtonsoft.Json;

namespace SonarQube.Client.Helpers
{
    public static class JsonHelper
    {
        public static T Deserialize<T>(string json)
        {
            using (var reader = new StringReader(json))
            {
                using (var textReader = new JsonTextReader(reader))
                {
                    return JsonSerializer.CreateDefault().Deserialize<T>(textReader);
                }
            }
        }

        public static string Serialize(object item)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                using (var textWriter = new JsonTextWriter(writer))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(textWriter, item);
                }
            }

            return sb.ToString();
        }
    }
}
