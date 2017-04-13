/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Verification helper class
    /// </summary>
    public static class VerificationHelper
    {
        /// <summary>
        /// Verifies that the <see cref="ProgressControllerStep"/> was initialized correctly
        /// </summary>
        /// <param name="testSubject">The step to verify</param>
        /// <param name="attributes">Step attributes</param>
        /// <param name="displayText">Step display text</param>
        public static void VerifyInitialized(ProgressControllerStep testSubject, StepAttributes attributes, string displayText = null)
        {
            StepExecution expectedExecution = (attributes & StepAttributes.BackgroundThread) != 0 ? StepExecution.BackgroundThread : StepExecution.ForegroundThread;
            bool expectedHidden = (attributes & StepAttributes.Hidden) != 0 ? true : false;
            bool expectedCancellable = (attributes & StepAttributes.NonCancellable) != 0 ? false : true;
            bool expectedImpactingProgress = (attributes & StepAttributes.NoProgressImpact) != 0 ? false : true;
            bool expectedIndeterminate = (attributes & StepAttributes.Indeterminate) != 0 ? true : false;

            CheckState(testSubject, StepExecutionState.NotStarted);
            testSubject.DisplayText.Should().Be(displayText, "Unexpected display text");
            testSubject.Cancellable.Should().Be(expectedCancellable, "Cancellable: Unexpected post initialization value");
            testSubject.Indeterminate.Should().Be(expectedIndeterminate, "Indeterminate: Unexpected post initialization value");
            testSubject.Execution.Should().Be(expectedExecution, "Execution: Unexpected post initialization value");
            testSubject.Hidden.Should().Be(expectedHidden, "Hidden: Unexpected post initialization value");
            testSubject.ImpactsProgress.Should().Be(expectedImpactingProgress, "ImpactingProgress: Unexpected post initialization value");

            if (expectedIndeterminate)
            {
                ProgressControllerHelper.IsIndeterminate(testSubject.Progress).Should().BeTrue("Progess: Should be Indeterminate");
            }
            else
            {
                testSubject.Progress.Should().Be(0, "Progress: Unexpected post initialization value");
            }
        }

        /// <summary>
        /// Checks the current state of a <see cref="IProgressStep"/>
        /// </summary>
        /// <param name="testSubject">The step to check</param>
        /// <param name="expectedState">The expected state of the step</param>
        public static void CheckState(IProgressStep testSubject, StepExecutionState expectedState)
        {
            testSubject.ExecutionState.Should().Be(expectedState, "Unexpected state");
        }
    }
}