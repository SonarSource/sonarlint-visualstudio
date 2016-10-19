//-----------------------------------------------------------------------
// <copyright file="ProgressViewModelTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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

            ViewModelVerifier.RunVerificationTest<ProgressViewModel, double>(testSubject, "Value", double.NaN, 1.0);
            ViewModelVerifier.RunVerificationTest<ProgressViewModel, bool>(testSubject, "IsIndeterminate", true, false);
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
            // Setup
            ProgressViewModel testSubject = new ProgressViewModel();

            // Sanity
            Assert.AreEqual(0, testSubject.Value, "Default value expected");

            // Act + Verify

            // Erroneous cases
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(double.NegativeInfinity));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(double.PositiveInfinity));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(0 - double.Epsilon));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.SetUpperBoundLimitedValue(1.0 + ProgressViewModel.UpperBoundMarginalErrorSupport + ProgressViewModel.UpperBoundMarginalErrorSupport));

            // Sanity
            Assert.AreEqual(0.0, testSubject.Value, "Erroneous cases should not change the default value");

            // NaN supported
            testSubject.SetUpperBoundLimitedValue(double.NaN);
            Assert.AreEqual(double.NaN, testSubject.Value);

            // Zero in range
            testSubject.SetUpperBoundLimitedValue(0);
            Assert.AreEqual(0.0, testSubject.Value);

            // One is in range
            testSubject.SetUpperBoundLimitedValue(1);
            Assert.AreEqual(1.0, testSubject.Value);

            // Anything between zero and one is in range
            Random r = new Random();
            double val = r.NextDouble();
            testSubject.SetUpperBoundLimitedValue(val);
            Assert.AreEqual(val, testSubject.Value);

            // More than one (i.e floating point summation errors) will become one
            testSubject.SetUpperBoundLimitedValue(1.0 + ProgressViewModel.UpperBoundMarginalErrorSupport);
            Assert.AreEqual(1.0, testSubject.Value);
        }
    }
}
