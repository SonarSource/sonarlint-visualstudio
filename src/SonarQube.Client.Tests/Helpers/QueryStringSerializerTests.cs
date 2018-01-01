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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SonarQube.Client.Helpers
{
    [TestClass]
    public class QueryStringSerializerTests
    {
        [TestMethod]
        public void Request_Null_Returns_Null()
        {
            QueryStringSerializer.ToQueryString(null).Should().BeNull();
        }

        [TestMethod]
        public void Simple_Properties_Test()
        {
            QueryStringSerializer.ToQueryString(
                new SimpleProperties
                {
                    a = 5,
                    b = "x y z",
                    c = new DateTime(2017, 10, 20, 14, 39, 25, DateTimeKind.Utc),
                    d = 10.5,
                    e = 20.6F,
                    f = 30.7M,
                    g = true
                }).Should().Be("a=5&b=x+y+z&c=2017-10-20T14%3a39%3a25.0000000Z&d=10.5&e=20.6&f=30.7&g=true");
        }

        [TestMethod]
        public void Collection_Properties_Test()
        {
            QueryStringSerializer.ToQueryString(new ArrayProperties
            {
                a = new[] { 1, 2, 3 },
                b = new[] { "x", "y", "z" },
                c = new List<bool> { true, false }
            }).Should().Be("a=1&a=2&a=3&b=x&b=y&b=z&c=true&c=false");
        }

        [TestMethod]
        public void Object_Properties_Throw_InvalidOperationExcepion()
        {
            Action action = () => serializer.ToQueryString(new InnerObject());
            action.Should().ThrowExactly<NotSupportedException>();
        }

        [TestMethod]
        public void JsonProperty_Overrides_Property_Name()
        {
            QueryStringSerializer.ToQueryString(new JsonAttributes { MyProperty = 10 })
                .Should().Be("a=10");
        }

        [TestMethod]
        public void Optional_Properties_Not_Serialized()
        {
            QueryStringSerializer.ToQueryString(new OptionalProperties())
                .Should().Be("");
        }

        [TestMethod]
        public void Non_Public_Properties_Not_Serialized()
        {
            QueryStringSerializer.ToQueryString(new PrivateProperties())
                .Should().Be("");
        }

        [TestMethod]
        public void Anonymous_Objects_Serialized()
        {
            QueryStringSerializer.ToQueryString(new { a = 10, b = true, c = new[] { "x", "y" } })
                .Should().Be("a=10&b=true&c=x&c=y");
        }

        public class SimpleProperties
        {
            public int a { get; set; }
            public string b { get; set; }
            public DateTime c { get; set; }
            public double d { get; set; }
            public float e { get; set; }
            public decimal f { get; set; }
            public bool g { get; set; }
        }

        public class ArrayProperties
        {
            public int[] a { get; set; }
            public string[] b { get; set; }
            public IEnumerable<bool> c { get; set; }
        }

        public class InnerObject
        {
            public SimpleProperties a { get; set; } = new SimpleProperties();
        }

        public class JsonAttributes
        {
            [JsonProperty("a")]
            public int MyProperty { get; set; }
        }

        public class OptionalProperties
        {
            public int? a { get; set; }
            public string b { get; set; }
        }

        public class PrivateProperties
        {
            private int a { get; set; }
            protected int b { get; set; }
            internal int c { get; set; }
        }
    }
}
