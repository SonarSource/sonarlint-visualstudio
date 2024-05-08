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

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers
{
    [TestClass]
    public class InterfaceToConcreteTypeConverterTests
    {
        [TestMethod]
        public void Deserialize_WithoutConverters_Fails()
        {
            var data = @"{""Children"":[{""Name"":""aaa""},{""Name"":""bbb""}]}";

            Action act = () => JsonConvert.DeserializeObject<Parent>(data);

            act.Should().Throw<JsonSerializationException>();
        }

        [TestMethod]
        public void Deserialize_WithConverters_Succeeds()
        {
            var data = @"{""Children"":[{""Name"":""aaa""},{""Name"":""bbb""}]}";

            var settings = new JsonSerializerSettings
            {
                Converters = {
                    new InterfaceToConcreteTypeConverter<IParent, Parent>(),
                    new InterfaceToConcreteTypeConverter<IChild, Child>()
                }
            };

            var actual = JsonConvert.DeserializeObject<IParent>(data, settings);

            actual.Should().NotBeNull();
            actual.Children.Should().HaveCount(2);
            actual.Children[0].Name.Should().Be("aaa");
            actual.Children[1].Name.Should().Be("bbb");
        }

        [TestMethod]
        public void WriteJson_ShouldThrow()
        {
            var testSubject = new InterfaceToConcreteTypeConverter<IParent, Parent>();

            JsonTextWriter writer = new JsonTextWriter(new StringWriter());

            Action act = () => testSubject.WriteJson(writer, new Parent(null), new JsonSerializer());

            act.Should().Throw<NotImplementedException>();
        }
    }

    public interface IParent
    {
        IReadOnlyList<IChild> Children { get; }
    }

    public interface IChild
    {
        string Name { get; }
    }

    public class Parent : IParent
    {
        public Parent(IReadOnlyList<IChild> children)
            => Children = children;

        public IReadOnlyList<IChild> Children { get; }
    }

    public class Child : IChild
    {
        public string Name { get; set; }
    }
}
