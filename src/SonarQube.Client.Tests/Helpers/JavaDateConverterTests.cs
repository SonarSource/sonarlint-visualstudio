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

using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SonarQube.Client.Helpers
{
    [TestClass]
    public class JavaDateConverterTests
    {
        [TestMethod]
        public void CanConvert_DateTimeOffset()
        {
            new JavaDateConverter().CanConvert(typeof(DateTimeOffset)).Should().BeTrue();
            new JavaDateConverter().CanConvert(typeof(DateTime)).Should().BeFalse();
            new JavaDateConverter().CanConvert(typeof(bool)).Should().BeFalse();
            new JavaDateConverter().CanConvert(typeof(int)).Should().BeFalse();
            new JavaDateConverter().CanConvert(typeof(object)).Should().BeFalse();
            new JavaDateConverter().CanConvert(typeof(string)).Should().BeFalse();
            new JavaDateConverter().CanConvert(typeof(TimeSpan)).Should().BeFalse();
        }

        [TestMethod]
        public void CanRead()
        {
            new JavaDateConverter().CanRead.Should().BeFalse();
        }

        [TestMethod]
        public void ReadJson()
        {
            Action action = () => new JavaDateConverter().ReadJson(null, null, null, null);
            action.ShouldThrow<NotSupportedException>();
        }

        [TestMethod]
        public void CanConvert_WriteJson()
        {
            // Arrange
            var sb = new StringBuilder();
            var writer = new JsonTextWriter(new StringWriter(sb));
            var serializer = JsonSerializer.CreateDefault();

            // Act
            var date = new DateTimeOffset(2017, 10, 20, 17, 35, 59, TimeSpan.FromHours(2));
            new JavaDateConverter().WriteJson(writer, date, serializer);

            // Assert
            sb.ToString().Should().Be("\"2017-10-20T17:35:59+0200\"");
        }
    }
}
