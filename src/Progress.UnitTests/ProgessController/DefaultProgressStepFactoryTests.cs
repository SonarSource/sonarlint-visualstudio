/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Progress.Controller;

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

        #endregion Test plumbing

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
            IProgressStepOperation stepOperation = this.testSubject.CreateStepOperation(this.controller, new ProgressStepDefinition("text", StepAttributes.None, (c, n) => { }));
            stepOperation.Should().NotBeNull("Expecting IProgressStepOperation");
            ProgressControllerStep step = stepOperation as ProgressControllerStep;
            step.Should().NotBeNull("Expecting ProgressControllerStep");

            VerificationHelper.CheckState(stepOperation.Step, StepExecutionState.NotStarted);

            IProgressStepExecutionEvents notifier = ((IProgressStepFactory)this.testSubject).GetExecutionCallback(stepOperation);
            stepOperation.Should().NotBeNull("Expecting IProgressStepExecutionEvents");
            notifier.Should().Be(step, "Expecting ProgressControllerStep");
        }
    }
}