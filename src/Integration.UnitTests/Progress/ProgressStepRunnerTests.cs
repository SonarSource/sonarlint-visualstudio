//-----------------------------------------------------------------------
// <copyright file="ProgressStepRunnerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Observation;
using System;

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
            // Setup
            ConfigurableProgressEvents progressEvents = new ConfigurableProgressEvents();
            ProgressControllerResult? result = null;
            Action<ProgressControllerResult> action = (r) => result = r;

            foreach (ProgressControllerResult progressResult in Enum.GetValues(typeof(ProgressControllerResult)))
            {
                result = null;
                Helpers.RunOnFinished(progressEvents, action);

                // Act
                progressEvents.SimulateFinished(progressResult);

                // Verify
                Assert.AreEqual(progressResult, result, "Action was not called");
                progressEvents.AssertNoFinishedEventHandlers();
            }
        }

        [TestMethod]
        public void ProgressStepRunner_Observe()
        {
            // Setup
            ConfigurableProgressController controller = new ConfigurableProgressController();
            ConfigurableProgressControlHost host = new ConfigurableProgressControlHost();
            controller.AddSteps(new ConfigurableProgressStep());// Needs at least one

            // Act
            using (ProgressObserver observer1 = ProgressStepRunner.Observe(controller, host))
            {
                // Verify
                Assert.IsNotNull(observer1, "Unexpected return value");
                Assert.AreSame(observer1, ProgressStepRunner.ObservedControllers[controller]);
                host.AssertHasProgressControl();
            }
        }

        [TestMethod]
        public void ProgressStepRunner_ChangeHost()
        {
            // Setup
            ConfigurableProgressController controller = new ConfigurableProgressController();
            controller.AddSteps(new ConfigurableProgressStep());// Needs at least one
            ConfigurableProgressControlHost host1 = new ConfigurableProgressControlHost();
            ProgressObserver observer = ProgressStepRunner.Observe(controller, host1);

            // Act
            ConfigurableProgressControlHost host2 = new ConfigurableProgressControlHost();
            ProgressStepRunner.ChangeHost(host2);

            // Verify
            using (var newObserver = ProgressStepRunner.ObservedControllers[controller])
            {
                Assert.IsNotNull(newObserver);
                Assert.AreNotSame(newObserver, observer);
                Assert.AreSame(observer.State, newObserver.State, "State was not transferred");
                host2.AssertHasProgressControl();
            }
        }

        [TestMethod]
        public void ProgressStepRunner_AbortAll()
        {
            // Setup
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

            // Verify
            controller1.AssertNumberOfAbortRequests(1);
            controller2.AssertNumberOfAbortRequests(1);
        }
    }
}
