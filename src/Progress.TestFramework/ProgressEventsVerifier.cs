//-----------------------------------------------------------------------
// <copyright file="ProgressEventsVerifier.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

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
            Assert.AreEqual(expectedChanges, this.cancellableStateChanges, "Unexpected cancellation state changes");
        }

        public void AssertCorrectExecution(ProgressControllerResult result)
        {
            Assert.IsTrue(this.started, "Invalid execution: Started didn't fire");
            Assert.AreEqual(result, this.executionResult, "Invalid execution: Finished didn't fire with the expect result ({0})", this.executionResult.HasValue ? this.executionResult.Value.ToString() : "NONE");
        }

        public void AssertStepCorrectExecution(IProgressStep step, StepExecutionState finalState)
        {
            if (finalState == StepExecutionState.NotStarted)
            {
                Assert.IsFalse(this.executionChanges.ContainsKey(step), "Not expecting any changes for a step that was not started");
            }
            else
            {
                List<StepExecutionChangedEventArgs> changes = this.executionChanges[step];
                Assert.IsNotNull(changes, "Cannot find the changes list for the specified step");
                VerifyStateTransitions(changes.Select(e => e.State).ToArray(), finalState);
            }
        }

        public void AssertExecutionProgress(IProgressStep step, params Tuple<string, double>[] expectedSequence)
        {
            List<StepExecutionChangedEventArgs> changes = this.executionChanges[step];
            Assert.IsNotNull(changes, "Cannot find the changes list for the specified step");
            Tuple<string, double>[] actualSequence = changes.Where(c => c.State == StepExecutionState.Executing).Select(c => Tuple.Create(c.ProgressDetailText, c.Progress)).ToArray();
            VerifyProgressSequence(!step.Indeterminate, expectedSequence, actualSequence);
        }

        private static void VerifyProgressSequence(bool determinate, Tuple<string, double>[] expectedSequence, Tuple<string, double>[] actualSequence)
        {
            // There's an extra executing notification for the transition from NotStarted -> Executing
            Assert.AreEqual(expectedSequence.Length + 1, actualSequence.Length, "Unexpected sequence length");
            Assert.IsNull(actualSequence[0].Item1, "The default transition should be with null display progress text");
            if (determinate)
            {
                Assert.AreEqual(0.0, actualSequence[0].Item2, "For determinate steps the initial percentage is 0%");
            }
            else
            {
                Assert.IsTrue(ProgressControllerHelper.IsIndeterminate(actualSequence[0].Item2), "Should be indeterminate");
            }

            for (int i = 0; i < expectedSequence.Length; i++)
            {
                Assert.AreEqual(expectedSequence[i], actualSequence[i + 1], "Unexpected sequence item");
            }
        }

        private static void VerifyStateTransitions(StepExecutionState[] transition, StepExecutionState finalState)
        {
            for (int i = 0; i < transition.Length; i++)
            {
                if (IsFinalState(transition[i]))
                {
                    Assert.AreEqual(finalState, transition[i], "Unexpected final state");
                    Assert.AreEqual(transition.Length - 1, i, "Final state should be the last one recorded");
                }
                else
                {
                    Assert.AreEqual(StepExecutionState.Executing, transition[i], "Only Executing is expected");
                }
            }
        }

        private static bool IsFinalState(StepExecutionState state)
        {
            return state == StepExecutionState.Cancelled || state == StepExecutionState.Failed || state == StepExecutionState.Succeeded;
        }
        #endregion

        #region Event handlers
        private static void AssertEventHandlerArgsNotNull(object sender, EventArgs e)
        {
            Assert.IsNotNull(sender, "sender should not be null");
            Assert.IsNotNull(e, "e should not be null");
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
        #endregion
    }
}
