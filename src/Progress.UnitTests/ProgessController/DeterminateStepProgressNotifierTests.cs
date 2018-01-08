/*
 * SonarLint for Visual Studio
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller;

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
            // Arrange
            var testSubject = new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), 11);

            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.IncrementProgress(0));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.IncrementProgress(-1));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => testSubject.IncrementProgress(12));

            // Check successful case (the last valid one)
            testSubject.IncrementProgress(11);
            testSubject.CurrentValue.Should().Be(11);
        }

        [TestMethod]
        public void DeterminateStepProgressNotifier_NotifyCurrentProgress()
        {
            // Arrange
            var controller = new ConfigurableProgressController(null);
            const int Steps = 2;
            var testSubject = new DeterminateStepProgressNotifier(controller, Steps);
            List<Tuple<string, double>> expectedProgress = new List<Tuple<string, double>>();

            // Act + Assert
            for (int i = 0; i < Steps; i++)
            {
                expectedProgress.Add(Tuple.Create("hello world " + i, 0.0));
                testSubject.NotifyCurrentProgress(expectedProgress.Last().Item1);

                testSubject.CurrentValue.Should().Be(0, "Should not change");
                controller.progressChanges.Should().Equal(expectedProgress);
            }
        }

        [TestMethod]
        public void DeterminateStepProgressNotifier_IncrementProgress()
        {
            // Arrange
            var controller = new ConfigurableProgressController(null);
            const int Steps = 227; // Quite a few values for which N * (1 / N) > 1.0
            var testSubject = new DeterminateStepProgressNotifier(controller, Steps);
            List<Tuple<string, double>> expectedProgress = new List<Tuple<string, double>>();

            // Act + Assert
            int expectedValue = 0;
            int i = 0;
            while(expectedValue < Steps)
            {
                int increment = i % 2 == 0 ? 2 : 1;
                i++;
                expectedValue += increment;
                testSubject.IncrementProgress(increment);

                testSubject.CurrentValue.Should().Be(expectedValue);
            }
        }

        [TestMethod]
        public void DeterminateStepProgressNotifier_NotifyIncrementedProgress()
        {
            // Arrange
            var controller = new ConfigurableProgressController(null);
            const int Steps = 11; // there are two numbers (9 and 11) for which N * (1 / N) > 1.0
            var testSubject = new DeterminateStepProgressNotifier(controller, Steps);
            List<Tuple<string, double>> expectedProgress = new List<Tuple<string, double>>();

            // Act + Assert
            for (int i = 0; i < Steps; i++)
            {
                int incrementedStepValue = i + 1;
                double progress = incrementedStepValue == Steps ? 1.0 : (double)incrementedStepValue / Steps;
                expectedProgress.Add(Tuple.Create("hello world " + i, progress));

                testSubject.CurrentValue.Should().Be(i);
                testSubject.NotifyIncrementedProgress(expectedProgress.Last().Item1);
                testSubject.CurrentValue.Should().Be(incrementedStepValue);

                controller.progressChanges.Should().Equal(expectedProgress);
            }
        }
    }
}