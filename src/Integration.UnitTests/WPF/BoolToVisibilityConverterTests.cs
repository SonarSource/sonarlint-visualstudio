//-----------------------------------------------------------------------
// <copyright file="BoolToVisibilityConverterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.WPF;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class BoolToVisibilityConverterTests
    {
        [TestMethod]
        public void BoolToVisibilityConverter_DefaultValues()
        {
            var converter = new BoolToVisibilityConverter();

            Assert.AreEqual(converter.TrueValue, Visibility.Visible);
            Assert.AreEqual(converter.FalseValue, Visibility.Collapsed);
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_NonBoolInput_ThrowsArgumentException()
        {
            var converter = new BoolToVisibilityConverter();

            Exceptions.Expect<ArgumentException>(() =>
            {
                converter.Convert("NotABoolean", typeof(Visibility), null, CultureInfo.InvariantCulture);
            });
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_NonVisibilityOutput_ThrowsArgumentException()
        {
            var converter = new BoolToVisibilityConverter();
            var notVisibilityType = typeof(string);

            Exceptions.Expect<ArgumentException>(() =>
            {
                converter.Convert(true, notVisibilityType, null, CultureInfo.InvariantCulture);
            });
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_True_ReturnsTrueValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

            Assert.IsInstanceOfType(result, typeof(Visibility));
            Assert.AreEqual(result, Visibility.Hidden);
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_False_ReturnsFalseValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

            Assert.IsInstanceOfType(result, typeof(Visibility));
            Assert.AreEqual(result, Visibility.Visible);
        }
    }
}
