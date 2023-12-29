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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SonarLint.VisualStudio.SLCore.Protocol
{
    /// <summary>
    ///     <see cref="JsonConverter" /> for <see cref="Either{TLeft,TRight}" /> type.
    ///     Left and Right are serialized to and deserialize from the same json property.
    /// </summary>
    /// <typeparam name="TLeft">A class that converts to json object. Primitive types (int, string, etc.) are not supported</typeparam>
    /// <typeparam name="TRight">A class that converts to json object. Primitive types (int, string, etc.) are not supported</typeparam>
    public class EitherJsonConverter<TLeft, TRight> : JsonConverter
        where TLeft : class
        where TRight : class
    {
        private readonly HashSet<string> leftProperties;
        private readonly HashSet<string> rightProperties;

        public EitherJsonConverter()
        {
            // todo check primitive types?
            leftProperties = GetAllPropertyAndFieldNames(typeof(TLeft));
            rightProperties = GetAllPropertyAndFieldNames(typeof(TRight));
            var intersection = leftProperties.Intersect(rightProperties).ToArray();
            leftProperties.ExceptWith(intersection);
            rightProperties.ExceptWith(intersection);
        }

        private static HashSet<string> GetAllPropertyAndFieldNames(Type type)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            return type.GetProperties(bindingFlags).Select(x => x.Name)
                .Concat(type.GetFields(bindingFlags).Select(x => x.Name))
                .ToHashSet();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var either = value as Either<TLeft, TRight>;

            if (either.Left != null)
            {
                serializer.Serialize(writer, either.Left);
                return;
            }

            serializer.Serialize(writer, either.Right);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Either<TLeft, TRight>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jToken = JToken.ReadFrom(reader);

            if (jToken.Type != JTokenType.Object)
            {
                // ??    
                throw new InvalidOperationException();
            }

            foreach (var jsonProperty in jToken.Children().Select(x => x.Path))
            {
                if (leftProperties.Contains(jsonProperty))
                {
                    return Either<TLeft, TRight>.CreateLeft(jToken.ToObject<TLeft>());
                }

                if (rightProperties.Contains(jsonProperty))
                {
                    return Either<TLeft, TRight>.CreateRight(jToken.ToObject<TRight>());
                }
            }

            throw new InvalidOperationException();
        }
    }
}
