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
using SonarLint.VisualStudio.Progress.Controller;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    public class DeterminateStepProgressNotifierTests
    {
        [Fact]
        public void Ctor_WhenGivingNullExecutionEvents_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new DeterminateStepProgressNotifier(null, 1);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("executionEvents"); ;
        }

        [Fact]
        public void Ctor_WhenGivingNumberOfStepslessThan1_ThrowsArgumentOutOfRangeException()
        {
            // Act
            Action act = () => new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), 0);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("numberOfIncrements");
        }

        [Fact]
        public void IncrementProgress_WhenGivingLessThanOne_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var testSubject = new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), 11);

            // Act
            Action act = () => testSubject.IncrementProgress(0);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("increment");
        }

        [Fact]
        public void IncrementProgress_WhenGivingMoreThanMax_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            int maxIncrements = 11;
            var testSubject = new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), maxIncrements);

            // Act
            Action act = () => testSubject.IncrementProgress(maxIncrements + 1);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("increment");
        }

        [Fact]
        public void IncrementProgress_WhenGivingCorrectValueOfIncrement_ReturnsExpectedValue()
        {
            // Arrange
            int maxIncrements = 11;
            var testSubject = new DeterminateStepProgressNotifier(new ConfigurableProgressController(null), maxIncrements);

            // Act
            testSubject.IncrementProgress(maxIncrements);

            // Assert
            testSubject.CurrentValue.Should().Be(maxIncrements);
        }

        [Fact]
        public void NotifyCurrentProgress_WhenIterating_RaiseExpectedEvents()
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

                testSubject.CurrentValue.Should().Be(0);
                controller.AssertProgressChangeEvents(expectedProgress);
            }
        }

        [Fact]
        public void NotifyCurrentProgress_WhenIterating_ReturnsExpectedValue()
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

        [Fact]
        public void NotifyIncrementedProgress_WhenIteraring_ReturnsExpectedValueAndRaiseEvent()
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

                controller.AssertProgressChangeEvents(expectedProgress);
            }
        }
    }
}