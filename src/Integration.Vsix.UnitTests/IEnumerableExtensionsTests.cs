//-----------------------------------------------------------------------
// <copyright file="IEnumerableExtensionsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Collections.Generic;

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
            // Act + Verify
            Assert.IsTrue(IEnumerableExtensions.AllEqual(new int[0]), "Expected empty enumerable to be AllEqual");
        }

        [TestMethod]
        public void IEnumerableExtensions_AllEqual_DefaultComparator_ValueTypes()
        {
            // Test case 1: same values
            // Act + Verify
            int[] sameValues = new[] { 1, 1, 1, 1, 1, 1, 1, 1 };
            Assert.IsTrue(IEnumerableExtensions.AllEqual(sameValues), "Expected same values to be AllEqual");

            // Test case 2: mixed values
            // Act + Verify
            int[] mixedValues = new[] { 1, 1, 1, 42, 1, 1, 1 };
            Assert.IsFalse(IEnumerableExtensions.AllEqual(mixedValues), "Expected mixed values NOT to be AllEqual");
        }

        [TestMethod]
        public void IEnumerableExtensions_AllEqual_DefaultComparator_ReferenceTypes()
        {
            // Setup
            var objA = new object();
            var objB = new object();

            // Test case 1: same instances
            // Act + Verify
            object[] sameInstances = new[] { objA, objA, objA, objA, objA };
            Assert.IsTrue(IEnumerableExtensions.AllEqual(sameInstances), "Expected same instances to be AllEqual");

            // Test case 2: mixed instances
            // Act + Verify
            object[] mixedInstances = new[] { objA, objB, objB, objA, objB };
            Assert.IsFalse(IEnumerableExtensions.AllEqual(mixedInstances), "Expected mixed instances NOT to be AllEqual");
        }

        [TestMethod]
        public void IEnumerableExtensions_AllEqual_CustomComparator()
        {
            // Setup
            var str1a = "mIxEdCaSeStRiNg";
            var str1b = "MiXeDcAsEsTrInG";
            var str2 = "another-string";

            // Test case 1: comparator equal
            // Act + Verify
            Assert.IsTrue(IEnumerableExtensions.AllEqual(new[] { str1a, str1b }, StringComparer.OrdinalIgnoreCase), "Expected to be AllEqual");

            // Test case 2: comparator not equal
            // Act + Verify
            Assert.IsFalse(IEnumerableExtensions.AllEqual(new[] { str1a, str1b }, StringComparer.Ordinal), "Expected to NOT be AllEqual");

            // Test case 3: values different
            // Act + Verify
            Assert.IsFalse(IEnumerableExtensions.AllEqual(new[] { str1a, str2 }, StringComparer.OrdinalIgnoreCase), "Expected to NOT be AllEqual");
        }
    }
}
