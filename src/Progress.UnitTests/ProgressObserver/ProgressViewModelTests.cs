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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class ProgressViewModelTests
    {
        [TestMethod]
        [Description("Verifies that all the publicly settable properties in ProgressViewModel notify changes")]
        public void ProgressViewModel_AllPublicPropertiesNotifyChanges()
        {
            ProgressViewModel testSubject = new ProgressViewModel();

            ViewModelVerifier.RunVerificationTest(testSubject, "Value", double.NaN, 1.0);
            ViewModelVerifier.RunVerificationTest(testSubject, "IsIndeterminate", true, false);
        }

        [TestMethod]
        [Description("Verifies all the exceptions that can be thrown from ProgressViewModel when setting invalid value")]
        public void ProgressViewModel_ArgChecks()
        {
            ProgressViewModel testSubject = new ProgressViewModel();

            // Setting the main progress with values out of [0..1] range will throw
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.Value = double.NegativeInfinity);
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.Value = double.PositiveInfinity);
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.Value = 0.0 - double.Epsilon);
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.Value = 1.00001);

            // Valid
            testSubject.Value = 0;
            testSubject.Value = 0.5;
            testSubject.Value = 1.0;
            testSubject.Value = double.NaN;
        }

        [TestMethod]
        public void ProgressViewModel_SetUpperBoundLimitedValue()
        {
            // Arrange
            ProgressViewModel testSubject = new ProgressViewModel();

            // Sanity
            testSubject.Value.Should().Be(0, "Default value expected");

            // Act + Assert

            // Erroneous cases
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(double.NegativeInfinity));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(double.PositiveInfinity));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(0 - double.Epsilon));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(1.0 + ProgressViewModel.UpperBoundMarginalErrorSupport + ProgressViewModel.UpperBoundMarginalErrorSupport));

            // Sanity
            testSubject.Value.Should().Be(0.0, "Erroneous cases should not change the default value");

            // NaN supported
            testSubject.SetUpperBoundLimitedValue(double.NaN);
            testSubject.Value.Should().Be(double.NaN);

            // Zero in range
            testSubject.SetUpperBoundLimitedValue(0);
            testSubject.Value.Should().Be(0.0);

            // One is in range
            testSubject.SetUpperBoundLimitedValue(1);
            testSubject.Value.Should().Be(1.0);

            // Anything between zero and one is in range
            Random r = new Random();
            double val = r.NextDouble();
            testSubject.SetUpperBoundLimitedValue(val);
            testSubject.Value.Should().Be(val);

            // More than one (i.e floating point summation errors) will become one
            testSubject.SetUpperBoundLimitedValue(1.0 + ProgressViewModel.UpperBoundMarginalErrorSupport);
            testSubject.Value.Should().Be(1.0);
        }
    }
}