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
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers;

[TestClass]
public class ImmutableDictionaryIgnoreCaseConverterTests
{
    internal class TestClass
    {
        [JsonConverter(typeof(ImmutableDictionaryIgnoreCaseConverter<string, string>))]
        public ImmutableDictionary<string, string> TestDictionary { get; set; }
    }

    [TestMethod]
    public void ReadJson_ShouldDeserializeAndCreateImmutableDictionaryWithIgnoreCase()
    {
        var json = "{\"TestDictionary\":{\"Key1\":\"Value1\",\"KEY2\":\"Value2\"}}";

        var result = JsonConvert.DeserializeObject<TestClass>(json);

        result.TestDictionary.Should().NotBeNull();
        result.TestDictionary.Should().HaveCount(2);
        result.TestDictionary["key1"].Should().Be("Value1");
        result.TestDictionary["KEY1"].Should().Be("Value1");
        result.TestDictionary["KEY2"].Should().Be("Value2");
        result.TestDictionary["key2"].Should().Be("Value2");
    }

    [TestMethod]
    public void WriteJson_ShouldSerializeCorrectly()
    {
        var expectedJson = "{\"TestDictionary\":{\"Key1\":\"Value1\",\"KEY2\":\"Value2\"}}";
        var testClass = new TestClass { TestDictionary = new Dictionary<string, string> { { "Key1", "Value1" }, { "KEY2", "Value2" } }.ToImmutableDictionary() };

        var result = JsonConvert.SerializeObject(testClass);

        result.Should().Be(expectedJson);
    }
}
