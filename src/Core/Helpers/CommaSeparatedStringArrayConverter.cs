/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Collections.Immutable;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.Helpers;

public class CommaSeparatedStringArrayConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is not IEnumerable<string> strings)
        {
            return;
        }

        var commaSeparatedList = string.Join(",", TrimValues(strings));
        writer.WriteValue(commaSeparatedList);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        if (reader.Value is null)
        {
            return existingValue; // return the default if not a valid value
        }

        if (reader.Value is not string commaSeparatedList)
        {
            throw new JsonException(
                string.Format(
                    CoreStrings.CommaSeparatedStringArrayConverter_UnexpectedType,
                    reader.Value.GetType(),
                    typeof(string),
                    reader.Path));
        }

        var values = commaSeparatedList.Split([','], StringSplitOptions.RemoveEmptyEntries);
        return TrimValues(values);
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(ImmutableArray<string>);

    private static ImmutableArray<string> TrimValues(IEnumerable<string> values) => values?.Select(value => value.Trim()).ToImmutableArray() ?? ImmutableArray<string>.Empty;
}
