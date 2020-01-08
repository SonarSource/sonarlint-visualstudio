/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IEnumerableExtensionsTests
    {
        [TestMethod]
        public void IEnumerableExtensions_AllEqual_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => IEnumerableExtensions.AllEqual(null, EqualityComparer<object>.Default));
            Exceptions.Expect<ArgumentNullException>(() => IEnumerableExtensions.AllEqual(new object[0], null));
        }

        [TestMethod]
        public void IEnumerableExtensions_AllEqual_Empty_IsTrue()
        {
            // Act + Assert
            IEnumerableExtensions.AllEqual(new int[0]).Should().BeTrue("Expected empty enumerable to be AllEqual");
        }

        [TestMethod]
        public void IEnumerableExtensions_AllEqual_DefaultComparator_ValueTypes()
        {
            // Test case 1: same values
            // Act + Assert
            int[] sameValues = new[] { 1, 1, 1, 1, 1, 1, 1, 1 };
            IEnumerableExtensions.AllEqual(sameValues).Should().BeTrue("Expected same values to be AllEqual");

            // Test case 2: mixed values
            // Act + Assert
            int[] mixedValues = new[] { 1, 1, 1, 42, 1, 1, 1 };
            IEnumerableExtensions.AllEqual(mixedValues).Should().BeFalse("Expected mixed values NOT to be AllEqual");
        }

        [TestMethod]
        public void IEnumerableExtensions_AllEqual_DefaultComparator_ReferenceTypes()
        {
            // Arrange
            var objA = new object();
            var objB = new object();

            // Test case 1: same instances
            // Act + Assert
            object[] sameInstances = new[] { objA, objA, objA, objA, objA };
            IEnumerableExtensions.AllEqual(sameInstances).Should().BeTrue("Expected same instances to be AllEqual");

            // Test case 2: mixed instances
            // Act + Assert
            object[] mixedInstances = new[] { objA, objB, objB, objA, objB };
            IEnumerableExtensions.AllEqual(mixedInstances).Should().BeFalse("Expected mixed instances NOT to be AllEqual");
        }

        [TestMethod]
        public void IEnumerableExtensions_AllEqual_CustomComparator()
        {
            // Arrange
            var str1a = "mIxEdCaSeStRiNg";
            var str1b = "MiXeDcAsEsTrInG";
            var str2 = "another-string";

            // Test case 1: comparator equal
            // Act + Assert
            IEnumerableExtensions.AllEqual(new[] { str1a, str1b }, StringComparer.OrdinalIgnoreCase).Should().BeTrue("Expected to be AllEqual");

            // Test case 2: comparator not equal
            // Act + Assert
            IEnumerableExtensions.AllEqual(new[] { str1a, str1b }, StringComparer.Ordinal).Should().BeFalse("Expected to NOT be AllEqual");

            // Test case 3: values different
            // Act + Assert
            IEnumerableExtensions.AllEqual(new[] { str1a, str2 }, StringComparer.OrdinalIgnoreCase).Should().BeFalse("Expected to NOT be AllEqual");
        }
    }
}