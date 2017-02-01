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

            // Act + Verify
            for (int i = 0; i < Steps; i++)
            {
                expectedProgress.Add(Tuple.Create("hello world " + i, 0.0));
                testSubject.NotifyCurrentProgress(expectedProgress.Last().Item1);

                testSubject.CurrentValue.Should().Be(0, "Should not change");
                controller.AssertProgressChangeEvents(expectedProgress);
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

            // Act + Verify
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

            // Act + Verify
            for (int i = 0; i < Steps; i++)
            {
                int incrementedStepValue = i + 1;
                double progress = incrementedStepValue == Steps ? 1.0 : (double)incrementedStepValue / Steps;
                expectedProgress.Add(Tuple.Create("hello world " + i, progress));

                testSubject.CurrentValue.Should().Be(i);
                testSubject.NotifyIncrementedProgress(expectedProgress.Last().Item1);
                testSubject.CurrentValue.Should().Be(incrementedStepValue);

                controller.AssertProgressChangeEvents(expectedProgress);
            }
        }
    }
}