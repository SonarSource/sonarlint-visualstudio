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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class FixedStepsProgressAdapterTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs_Throws()
        {
            // Arrange
            Action act = () => new FixedStepsProgressAdapter(null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("executionEvents");
        }

        [TestMethod]
        public void Progress_MultipleValidSteps()
        {
            // Arrange
            var listener = new ConfigurableProgressStepExecutionEvents();
            var testSubject = new FixedStepsProgressAdapter(listener);

            // Act
            Report(testSubject, "Starting...", 0, 4);
            Report(testSubject, "Step1", 1, 4);
            Report(testSubject, "Step1 again", 1, 4);
            Report(testSubject, null, 2, 4);
            Report(testSubject, "", 3, 4);
            Report(testSubject, "Ending...", 4, 4);

            listener.AssertProgressMessages("Starting...",
                "Step1",
                "Step1 again",
                null,
                "",
                "Ending...");

            listener.AssertProgress(
                0 / 4d,
                1 / 4d,
                1 / 4d,
                2 / 4d,
                3 / 4d,
                1.0);
        }

        [TestMethod]
        public void Progress_ChangeTotalSteps_Fails()
        {
            // Arrange
            var listener = new ConfigurableProgressStepExecutionEvents();
            var testSubject = new FixedStepsProgressAdapter(listener);

            // 1. Report first event
            Report(testSubject, "Starting...", 0, 3);

            // 2. Report second event, reporting a changed total step count
            Action act = () => Report(testSubject, null, 1, 4);
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("TotalSteps");
        }

        [TestMethod]
        public void Progress_ReduceCurrentStep_Fails()
        {
            // Arrange
            var listener = new ConfigurableProgressStepExecutionEvents();
            var testSubject = new FixedStepsProgressAdapter(listener);

            // 1. Report first event
            Report(testSubject, null, 2, 3);

            // 2. Report second event, reporting a lower current step
            Action act = () => Report(testSubject, null, 1, 3);
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("CurrentStep");
        }

        private static void Report(FixedStepsProgressAdapter testSubject, string message, int currentStep, int totalSteps)
        {
            var testSubjectAsIProgress = (IProgress<FixedStepsProgress>)testSubject;
            var progressData = new FixedStepsProgress(message, currentStep, totalSteps);
            testSubjectAsIProgress.Report(progressData);
        }
    }
}
