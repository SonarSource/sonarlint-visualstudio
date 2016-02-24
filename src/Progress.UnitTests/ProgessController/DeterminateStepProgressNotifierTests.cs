//-----------------------------------------------------------------------
// <copyright file="DeterminateStepProgressNotifierTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.UnitTests;
using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class DeterminateStepProgressNotifierTests
    {
        [TestMethod]
        public void DeterminateStepProgressNotifier_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new DeterminateStepProgressNotifier(null, 1));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), 0));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), -1));
        }

        [TestMethod]
        public void DeterminateStepProgressNotifier_IncrementProgress_ArgChecks()
        {
            // Setup
            var testSubject = new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), 11);

            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.IncrementProgress(0));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.IncrementProgress(-1));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.IncrementProgress(12));

            // Check successful case (the last valid one)
            testSubject.IncrementProgress(11);
            Assert.AreEqual(11, testSubject.CurrentValue);
        }

        [TestMethod]
        public void DeterminateStepProgressNotifier_NotifyCurrentProgress()
        {
            // Setup
            var controller = new ConfigurableProgressController(null);
            const int Steps = 2;
            var testSubject = new DeterminateStepProgressNotifier(controller, Steps);
            List<Tuple<string, double>> expectedProgress = new List<Tuple<string, double>>();

            // Act + Verify
            for (int i = 0; i < Steps; i++)
            {
                expectedProgress.Add(Tuple.Create("hello world " + i, 0.0));
                testSubject.NotifyCurrentProgress(expectedProgress.Last().Item1);

                Assert.AreEqual(0, testSubject.CurrentValue, "Should not change");
                controller.AssertProgressChangeEvents(expectedProgress);
            }
        }

        [TestMethod]
        public void DeterminateStepProgressNotifier_IncrementProgress()
        {
            // Setup
            var controller = new ConfigurableProgressController(null);
            const int Steps = 227; // Quite a few values for which N * (1 / N) > 1.0
            var testSubject = new DeterminateStepProgressNotifier(controller, Steps);
            List<Tuple<string, double>> expectedProgress = new List<Tuple<string, double>>();

            // Act + Verify
            int expectedValue = 0;
            int i = 0;
            while(expectedValue < Steps)
            {
                int increment = i % 2 == 0 ? 2 : 1;
                i++;
                expectedValue += increment;
                testSubject.IncrementProgress(increment);

                Assert.AreEqual(expectedValue, testSubject.CurrentValue);
            }
        }

        [TestMethod]
        public void DeterminateStepProgressNotifier_NotifyIncrementedProgress()
        {
            // Setup
            var controller = new ConfigurableProgressController(null);
            const int Steps = 11; // there are two numbers (9 and 11) for which N * (1 / N) > 1.0
            var testSubject = new DeterminateStepProgressNotifier(controller, Steps);
            List<Tuple<string, double>> expectedProgress = new List<Tuple<string, double>>();

            // Act + Verify
            for (int i = 0; i < Steps; i++)
            {
                int incrementedStepValue = i + 1;
                double progress = incrementedStepValue == Steps ? 1.0 : (double)incrementedStepValue / Steps;
                expectedProgress.Add(Tuple.Create("hello world " + i, progress));

                Assert.AreEqual(i, testSubject.CurrentValue);
                testSubject.NotifyIncrementedProgress(expectedProgress.Last().Item1);
                Assert.AreEqual(incrementedStepValue, testSubject.CurrentValue);

                controller.AssertProgressChangeEvents(expectedProgress);
            }
        }
    }
}
