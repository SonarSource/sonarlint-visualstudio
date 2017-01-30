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
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    public class DefaultProgressStepFactoryTests
    {
        [Fact]
        public void CreateStepOperation_WhenUsingInvalidInput_ThrowsInvalidOperationException()
        {
            // Arrange
            var testSubject = new DefaultProgressStepFactory();
            var controller = new ConfigurableProgressController(new ConfigurableServiceProvider());

            // Act
            Action act = () => testSubject.CreateStepOperation(controller, new StubProgressStepDefinition());

            // Assert
            act.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void GetExecutionCallback_WhenUsingInvalidInput_ThrowsInvalidOperationException()
        {
            // Arrange
            var testSubject = new DefaultProgressStepFactory();

            // Act
            Action act = () => testSubject.GetExecutionCallback(new StubProgressStepOperation());

            // Assert
            act.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void CreateStepOperation_WhenUsingProperInputs_IsInExpectedExecutionState()
        {
            // Arrange
            var testSubject = new DefaultProgressStepFactory();
            var controller = new ConfigurableProgressController(new ConfigurableServiceProvider());

            // Act
            var stepOperation = testSubject.CreateStepOperation(controller, new ProgressStepDefinition("text", StepAttributes.None, (c, n) => { }));
            var step = stepOperation as ProgressControllerStep;

            // Assert
            stepOperation.Should().NotBeNull();
            step.Should().NotBeNull();
            stepOperation.Step.ExecutionState.Should().Be(StepExecutionState.NotStarted);
        }

        [Fact]
        public void GetExecutionCallback_WhenUsingProperInputs_ReturnsExpectedStep()
        {
            // Arrange
            var testSubject = new DefaultProgressStepFactory();
            var controller = new ConfigurableProgressController(new ConfigurableServiceProvider());
            var stepOperation = testSubject.CreateStepOperation(controller, new ProgressStepDefinition("text", StepAttributes.None, (c, n) => { }));
            var step = stepOperation as ProgressControllerStep;

            // Act
            var notifier = ((IProgressStepFactory)testSubject).GetExecutionCallback(stepOperation);

            // Assert
            notifier.Should().NotBeNull();
            notifier.Should().Be(step);
        }
    }
}
