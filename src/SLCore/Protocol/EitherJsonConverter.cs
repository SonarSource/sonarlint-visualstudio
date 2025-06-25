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

using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SonarLint.VisualStudio.SLCore.Protocol
{
    /// <summary>
    ///     <see cref="JsonConverter" /> for <see cref="Either{TLeft,TRight}" /> type.
    ///     Left and Right are serialized to and deserialize from the same json property.
    /// </summary>
    /// <typeparam name="TLeft">A class that converts to json object. Primitive types and Arrays are not supported</typeparam>
    /// <typeparam name="TRight">A class that converts to json object. Primitive types and Arrays are not supported</typeparam>
    /// <remarks>
    /// <list type="bullet">
    /// <item><a href="https://www.newtonsoft.com/json/help/html/serializationguide.htm#PrimitiveTypes">Primitive types</a></item>
    /// <item><a href="https://www.newtonsoft.com/json/help/html/serializationguide.htm#ComplexTypes">Arrays (IList, IEnumerable, IList&lt;T&gt;, Array)</a></item>
    /// </list>
    /// </remarks>
    public class EitherJsonConverter<TLeft, TRight> : JsonConverter
        where TLeft : class
        where TRight : class
    {
        private readonly HashSet<string> leftProperties;
        private readonly HashSet<string> rightProperties;

        public EitherJsonConverter()
        {
            leftProperties = GetAllPropertyAndFieldNames(typeof(TLeft));
            rightProperties = GetAllPropertyAndFieldNames(typeof(TRight));
            var intersection = leftProperties.Intersect(rightProperties).ToArray();
            leftProperties.ExceptWith(intersection);
            rightProperties.ExceptWith(intersection);

            if (!rightProperties.Any() && !leftProperties.Any())
            {
                throw new ArgumentException(
                    string.Format(SLCoreStrings.EitherJsonConverter_EquivalentPropertiesExceptionMessage, typeof(TLeft), typeof(TRight)));
            }
        }

        private static HashSet<string> GetAllPropertyAndFieldNames(Type type)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            return type.GetProperties(bindingFlags).Select(x => x.Name)
                .Concat(type.GetFields(bindingFlags).Select(x => x.Name))
                .ToHashSet();
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var either = value as Either<TLeft, TRight>;

            if (either?.Left != null)
            {
                serializer.Serialize(writer, either.Left);
                return;
            }

            serializer.Serialize(writer, either?.Right);
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Either<TLeft, TRight>);

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            var jToken = JToken.ReadFrom(reader);
            if (jToken.Type != JTokenType.Object)
            {
                throw new InvalidOperationException($"Expected {JTokenType.Object}, found {jToken.Type}");
            }

            var jsonProperties = jToken.Children().Select(x => x.Path).ToList();
            var either = ChooseForNonEmptyProperties(jsonProperties, jToken) ?? ChooseForEmptyProperties(jsonProperties, jToken);

            return either ?? throw new InvalidOperationException(SLCoreStrings.EitherJsonConverter_NoDefinitiveChoiceExceptionMessage);
        }

        private Either<TLeft, TRight>? ChooseForNonEmptyProperties(List<string> jsonProperties, JToken jToken)
        {
            foreach (var jsonProperty in jsonProperties)
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
            return null;
        }

        private Either<TLeft, TRight>? ChooseForEmptyProperties(List<string> jsonProperties, JToken jToken)
        {
            if (!jsonProperties.Any())
            {
                if (!leftProperties.Any())
                {
                    return Either<TLeft, TRight>.CreateLeft(jToken.ToObject<TLeft>());
                }
                if (!rightProperties.Any())
                {
                    return Either<TLeft, TRight>.CreateRight(jToken.ToObject<TRight>());
                }
            }
            return null;
        }
    }
}
