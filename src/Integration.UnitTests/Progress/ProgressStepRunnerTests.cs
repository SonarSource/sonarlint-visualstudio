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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Observation;

namespace SonarLint.VisualStudio.Integration.UnitTests.Progress
{
    [TestClass]
    public class ProgressStepRunnerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ProgressStepRunner.Reset();
        }

        [TestMethod]
        public void ProgressStepRunner_OnFinished()
        {
            // Arrange
            ConfigurableProgressEvents progressEvents = new ConfigurableProgressEvents();
            ProgressControllerResult? result = null;
            Action<ProgressControllerResult> action = (r) => result = r;

            foreach (ProgressControllerResult progressResult in Enum.GetValues(typeof(ProgressControllerResult)))
            {
                result = null;
                Helpers.RunOnFinished(progressEvents, action);

                // Act
                progressEvents.SimulateFinished(progressResult);

                // Assert
                result.Should().Be(progressResult, "Action was not called");
                progressEvents.AssertNoFinishedEventHandlers();
            }
        }

        [TestMethod]
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
                ProgressStepRunner.ObservedControllers[controller].Should().Be(observer1);
                host.ProgressControl.Should().NotBeNull();
            }
        }

        [TestMethod]
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
                observer.Should().NotBe(newObserver);
                newObserver.State.Should().Be(observer.State, "State was not transferred");
                host2.ProgressControl.Should().NotBeNull();
            }
        }

        [TestMethod]
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
            controller1.NumberOfAbortRequests.Should().Be(1);
            controller2.NumberOfAbortRequests.Should().Be(1);
        }
    }
}