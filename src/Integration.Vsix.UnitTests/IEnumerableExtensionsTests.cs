/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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