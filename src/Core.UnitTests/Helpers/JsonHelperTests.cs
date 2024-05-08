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
using System.Linq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers
{
    [TestClass]
    public class JsonHelperTests
    {
        [TestMethod]
        public void TryDeserialize_CorrectFormat_ReturnsTrue()
        {
            var expectedParent = new TestParent { Children = new List<TestChild> { new TestChild(1, "string1", 0.3m), new TestChild(2, "string2", 2.3m) } };

            var jsonContent = JsonConvert.SerializeObject(expectedParent);

            var result = JsonHelper.TryDeserialize<TestParent>(jsonContent, out var actualParent);

            result.Should().BeTrue();
            actualParent.Should().NotBeNull();
            actualParent.Children.Count().Should().Be(2);
            for (int i = 0; i < 2; i++)
            {
                actualParent.Children[i].IntProperty.Should().Be(expectedParent.Children[i].IntProperty);
                actualParent.Children[i].StringProperty.Should().Be(expectedParent.Children[i].StringProperty);
                actualParent.Children[i].DecimalProperty.Should().Be(expectedParent.Children[i].DecimalProperty);
            }
        }

        [TestMethod]
        public void TryDeserialize_WrongFormat_ReturnsFalse()
        {
            var jsonContent = "Not a Correct Json";

            var result = JsonHelper.TryDeserialize<TestParent>(jsonContent, out var parent);

            result.Should().BeFalse();
            parent.Should().BeNull();
        }

        private class TestParent
        {
            public IList<TestChild> Children { get; set; }
        }

        private class TestChild
        {
            public TestChild(int intProperty, string stringProperty, decimal decimalProperty)
            {
                IntProperty = intProperty;
                StringProperty = stringProperty;
                DecimalProperty = decimalProperty;
            }

            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
            public decimal DecimalProperty { get; set; }
        }
    }    
}
