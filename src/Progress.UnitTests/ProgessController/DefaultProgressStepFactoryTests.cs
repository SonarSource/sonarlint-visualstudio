//-----------------------------------------------------------------------
// <copyright file="DefaultProgressStepFactoryTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class DefaultProgressStepFactoryTests
    {
        private DefaultProgressStepFactory testSubject;
        private ConfigurableProgressController controller;

        #region Test plumbing
        public TestContext TestContext
        {
            get;
            set;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.testSubject = new DefaultProgressStepFactory();
            this.controller = new ConfigurableProgressController(new ConfigurableServiceProvider());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testSubject = null;
            this.controller = null;
        }
        #endregion

        [TestMethod]
        [Description("Verifies that the factory implementation code handles correctly unsupported types")]
        public void SequentialProgressController_IProgressStepFactory_UnsupportedInputs()
        {
            Exceptions.Expect<InvalidOperationException>(() => this.testSubject.CreateStepOperation(this.controller, new StubProgressStepDefinition()));
            Exceptions.Expect<InvalidOperationException>(() => this.testSubject.GetExecutionCallback(new StubProgressStepOperation()));
        }

        [TestMethod]
        [Description("Verifies that the factory implementation code handles correctly the supported types")]
        public void SequentialProgressController_IProgressStepFactory_SupportedInputs()
        {
            IProgressStepOperation stepOperation = this.testSubject.CreateStepOperation(this.controller, new ProgressStepDefinition("text", StepAttributes.Default, (c, n) => { }));
            Assert.IsNotNull(stepOperation, "Expecting IProgressStepOperation");
            ProgressControllerStep step = stepOperation as ProgressControllerStep;
            Assert.IsNotNull(step, "Expecting ProgressControllerStep");

            VerificationHelper.CheckState(stepOperation.Step, StepExecutionState.NotStarted);

            IProgressStepExecutionEvents notifier = ((IProgressStepFactory)this.testSubject).GetExecutionCallback(stepOperation);
            Assert.IsNotNull(stepOperation, "Expecting IProgressStepExecutionEvents");
            Assert.AreSame(step, notifier, "Expecting ProgressControllerStep");
        }
    }
}
