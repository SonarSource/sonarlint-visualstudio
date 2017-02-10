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

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test helper to verify general <see cref="IProgressController"/> and <see cref="IProgressStepOperation"/> execution
    /// and also verification of the event mechanism <see cref="IProgressEvents"/>
    /// </summary>
    public class ProgressEventsVerifier
    {
        private readonly IProgressEvents events;
        private bool started;
        private ProgressControllerResult? executionResult;
        private readonly Dictionary<IProgressStep, List<StepExecutionChangedEventArgs>> executionChanges = new Dictionary<IProgressStep, List<StepExecutionChangedEventArgs>>();
        private int cancellableStateChanges = 0;

        public ProgressEventsVerifier(IProgressEvents events)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            this.events = events;

            events.Started += this.OnStarted;
            events.Finished += this.OnFinished;
            events.StepExecutionChanged += this.OnStepExecutionChanged;
            events.CancellationSupportChanged += this.OnCancellationSupportChanged;
        }

        #region Test helpers

        public void AssertCancellationChanges(int expectedChanges)
        {
            this.cancellableStateChanges.Should().Be(expectedChanges, "Unexpected cancellation state changes");
        }

        public void AssertCorrectExecution(ProgressControllerResult result)
        {
            this.started.Should().BeTrue("Invalid execution: Started didn't fire");
            this.executionResult.Should().Be(result, "Invalid execution: Finished didn't fire with the expect result ({0})", this.executionResult.HasValue ? this.executionResult.Value.ToString() : "NONE");
        }

        public void AssertStepCorrectExecution(IProgressStep step, StepExecutionState finalState)
        {
            if (finalState == StepExecutionState.NotStarted)
            {
                this.executionChanges.Should().NotContainKey(step, "Not expecting any changes for a step that was not started");
            }
            else
            {
                List<StepExecutionChangedEventArgs> changes = this.executionChanges[step];
                changes.Should().NotBeNull("Cannot find the changes list for the specified step");
                VerifyStateTransitions(changes.Select(e => e.State).ToArray(), finalState);
            }
        }

        public void AssertExecutionProgress(IProgressStep step, params Tuple<string, double>[] expectedSequence)
        {
            List<StepExecutionChangedEventArgs> changes = this.executionChanges[step];
            changes.Should().NotBeNull("Cannot find the changes list for the specified step");
            Tuple<string, double>[] actualSequence = changes.Where(c => c.State == StepExecutionState.Executing).Select(c => Tuple.Create(c.ProgressDetailText, c.Progress)).ToArray();
            VerifyProgressSequence(!step.Indeterminate, expectedSequence, actualSequence);
        }

        private static void VerifyProgressSequence(bool determinate, Tuple<string, double>[] expectedSequence, Tuple<string, double>[] actualSequence)
        {
            // There's an extra executing notification for the transition from NotStarted -> Executing
            actualSequence.Should().HaveCount(expectedSequence.Length + 1, "Unexpected sequence length");
            actualSequence[0].Item1.Should().BeNull("The default transition should be with null display progress text");
            if (determinate)
            {
                actualSequence[0].Item2.Should().Be(0.0, "For determinate steps the initial percentage is 0%");
            }
            else
            {
                ProgressControllerHelper.IsIndeterminate(actualSequence[0].Item2).Should().BeTrue("Should be indeterminate");
            }

            for (int i = 0; i < expectedSequence.Length; i++)
            {
                actualSequence[i + 1].Should().Be(expectedSequence[i], "Unexpected sequence item");
            }
        }

        private static void VerifyStateTransitions(StepExecutionState[] transition, StepExecutionState finalState)
        {
            for (int i = 0; i < transition.Length; i++)
            {
                if (IsFinalState(transition[i]))
                {
                    transition[i].Should().Be(finalState, "Unexpected final state");
                    i.Should().Be(transition.Length - 1, "Final state should be the last one recorded");
                }
                else
                {
                    transition[i].Should().Be(StepExecutionState.Executing, "Only Executing is expected");
                }
            }
        }

        private static bool IsFinalState(StepExecutionState state)
        {
            return state == StepExecutionState.Cancelled || state == StepExecutionState.Failed || state == StepExecutionState.Succeeded;
        }

        #endregion Test helpers

        #region Event handlers

        private static void AssertEventHandlerArgsNotNull(object sender, EventArgs e)
        {
            sender.Should().NotBeNull("sender should not be null");
            e.Should().NotBeNull("e should not be null");
        }

        private void OnCancellationSupportChanged(object sender, CancellationSupportChangedEventArgs e)
        {
            AssertEventHandlerArgsNotNull(sender, e);

            this.cancellableStateChanges++;
            // Satisfy the sequential controller verification code
            e.Handled();
        }

        private void OnStepExecutionChanged(object sender, StepExecutionChangedEventArgs e)
        {
            AssertEventHandlerArgsNotNull(sender, e);

            List<StepExecutionChangedEventArgs> list;
            if (!this.executionChanges.TryGetValue(e.Step, out list))
            {
                this.executionChanges[e.Step] = list = new List<StepExecutionChangedEventArgs>();
            }

            list.Add(e);
            // Satisfy the sequential controller verification code
            e.Handled();
        }

        private void OnFinished(object sender, ProgressControllerFinishedEventArgs e)
        {
            AssertEventHandlerArgsNotNull(sender, e);

            this.executionResult = e.Result;
            // Satisfy the sequential controller verification code
            e.Handled();
        }

        private void OnStarted(object sender, ProgressEventArgs e)
        {
            AssertEventHandlerArgsNotNull(sender, e);

            this.started = true;
            // Satisfy the sequential controller verification code
            e.Handled();
        }

        #endregion Event handlers
    }
}