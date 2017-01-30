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

using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Observation;
using System;
using Xunit;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Progress
{
    public class ProgressStepRunnerTests
    {
        public ProgressStepRunnerTests()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [Fact]
        public void ProgressStepRunner_OnFinished()
        {
            // Arrange
            ConfigurableProgressEvents progressEvents = new ConfigurableProgressEvents();
            ProgressControllerResult? result = null;
            Action<ProgressControllerResult> action = (r) => result = r;

            foreach (ProgressControllerResult progressResult in Enum.GetValues(typeof(ProgressControllerResult)))
            {
                result = null;
                VisualStudio.Progress.Controller.Helpers.RunOnFinished(progressEvents, action);

                // Act
                progressEvents.SimulateFinished(progressResult);

                // Assert
                progressResult.Should().Be( result, "Action was not called");
                progressEvents.AssertNoFinishedEventHandlers();
            }
        }

        [Fact]
        public void ProgressStepRunner_Observe()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            ConfigurableProgressControlHost host = new ConfigurableProgressControlHost();
            controller.AddSteps(new ConfigurableProgressStep());// Needs at least one

            // Act
            using (ProgressObserver observer1 = ProgressStepRunner.Observe(controller, host))
            {
                // Assert
                observer1.Should().NotBeNull("Unexpected return value");
                observer1.Should().Be(ProgressStepRunner.ObservedControllers[controller]);
                host.AssertHasProgressControl();
            }
        }

        [Fact]
        public void ProgressStepRunner_ChangeHost()
        {
            // Arrange
            ConfigurableProgressController controller = new ConfigurableProgressController();
            controller.AddSteps(new ConfigurableProgressStep());// Needs at least one
            ConfigurableProgressControlHost host1 = new ConfigurableProgressControlHost();
            ProgressObserver observer = ProgressStepRunner.Observe(controller, host1);

            // Act
            ConfigurableProgressControlHost host2 = new ConfigurableProgressControlHost();
            ProgressStepRunner.ChangeHost(host2);

            // Assert
            using (var newObserver = ProgressStepRunner.ObservedControllers[controller])
            {
                newObserver.Should().NotBeNull();
                newObserver.Should().Be(observer);
                newObserver.State.Should().Be(observer.State, "State was not transferred");
                host2.AssertHasProgressControl();
            }
        }

        [Fact]
        public void ProgressStepRunner_AbortAll()
        {
            // Arrange
            ConfigurableProgressController controller1 = new ConfigurableProgressController();
            controller1.AddSteps(new ConfigurableProgressStep());// Needs at least one
            ConfigurableProgressControlHost host1 = new ConfigurableProgressControlHost();
            ProgressObserver observer1 = ProgressStepRunner.Observe(controller1, host1);
            ConfigurableProgressController controller2 = new ConfigurableProgressController();
            controller2.AddSteps(new ConfigurableProgressStep());// Needs at least one
            ConfigurableProgressControlHost host2 = new ConfigurableProgressControlHost();
            ProgressObserver observer2 = ProgressStepRunner.Observe(controller2, host2);

            // Act
            ProgressStepRunner.AbortAll();

            // Assert
            controller1.AssertNumberOfAbortRequests(1);
            controller2.AssertNumberOfAbortRequests(1);
        }
    }
}
