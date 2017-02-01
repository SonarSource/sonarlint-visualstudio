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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class ProgressControllerStepTests
    {
        private const int DeterminateLoops = 10;
        private ConfigurableProgressController testController;
        private ProgressControllerStep testSubject;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurableServiceProvider sp = new ConfigurableServiceProvider();
            sp.RegisterService(typeof(SVsTaskSchedulerService), new SingleThreadedTaskSchedulerService());
            this.testController = new ConfigurableProgressController(sp);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testController.Dispose();
        }
        #endregion

        #region Tests
        [TestMethod]
        public void ProgressControllerStep_Constructor_ArgCheck()
        {
            // Args checks for ProgressControllerStep
            Exceptions.Expect<ArgumentNullException>(() => new ProgressControllerStep(null, null));
            Exceptions.Expect<ArgumentNullException>(() => new ProgressControllerStep(this.testController, null));
            Exceptions.Expect<ArgumentNullException>(() => new ProgressControllerStep(null, new ProgressStepDefinition("some text", StepAttributes.Hidden, (c, n) => { })));

            // Arg check for ProgressStepDefinition
            Exceptions.Expect<ArgumentNullException>(() => new ProgressStepDefinition("some text", StepAttributes.Hidden, null));
        }

        [TestMethod]
        [Description("Verifies the state transition from initialized to successful for all of the possible step attribute combinations")]
        public void ProgressControllerStep_States()
        {
            int maxFlag = ((int[])Enum.GetValues(typeof(StepAttributes))).Max();
            for (int i = 0; i <= maxFlag; i++)
            {
                StepAttributes attributes = (StepAttributes)i;
                string text = (attributes & StepAttributes.Hidden) != 0 ? null : Environment.TickCount.ToString();

                this.InitializeAndExecuteTestSubject(text, attributes, this.ExecuteAndVerify);

                // Assert
                VerificationHelper.CheckState(this.testSubject, StepExecutionState.Succeeded);
                this.testController.AssertNoProgressChangeEvents();
            }
        }

        [TestMethod]
        [Description("Verifies that the step updates the progress as expected")]
        public void ProgressControllerStep_ProgressUpdate()
        {
            // Arrange
            this.InitializeAndExecuteTestSubject("progress-update", StepAttributes.None, this.ExecuteAndNotify);

            // Assert
            this.testController.AssertProgressChangeEvents(GetExpectedExecutionEvents());
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Succeeded);
        }

        [TestMethod]
        [Description("Verifies that the step will fail in case of exception and the state will change to failed")]
        public void ProgressControllerStep_Failed()
        {
            // Arrange
            this.InitializeAndExecuteTestSubject("exception in executing a step operation", StepAttributes.None, this.ExecuteAndFail);

            // Assert
            this.testController.AssertNoProgressChangeEvents();
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Failed);
        }

        [TestMethod]
        [Description("Verifies that when the step is canceled it will change state to canceled")]
        public void ProgressControllerStep_Cancelled()
        {
            // Arrange
            this.InitializeAndExecuteTestSubject("canceled step operation", StepAttributes.None, this.ExecuteAndCancell);

            // Assert
            this.testController.AssertNoProgressChangeEvents();
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Cancelled);
        }

        [TestMethod]
        [Description("Verifies the non-cancellable behavior of the step")]
        public void ProgressControllerStep_NonCancellable()
        {
            this.InitializeAndExecuteTestSubject("non-cancellable step operation", StepAttributes.NonCancellable, this.ExecuteNonCancellable);

            // Assert
            this.testController.AssertNoProgressChangeEvents();
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Succeeded);
        }
        #endregion

        #region Test helpers
        private static List<Tuple<string, double>> GetExpectedExecutionEvents()
        {
            List<Tuple<string, double>> list = new List<Tuple<string, double>>();
            for (int i = 0; i < DeterminateLoops; i++)
            {
                list.Add(Tuple.Create(i.ToString(), (double)(i + 1) / (double)DeterminateLoops));
            }

            return list;
        }

        private void InitializeAndExecuteTestSubject(string text, StepAttributes attributes, Action<CancellationToken, IProgressStepExecutionEvents> operation)
        {
            // Arrange
            this.testSubject = new ProgressControllerStep(this.testController, new ProgressStepDefinition(text, attributes, operation));

            // Assert initialized state
            VerificationHelper.VerifyInitialized(this.testSubject, attributes, text);

            // Act by the controller
            this.testController.Execute(this.testSubject);
        }

        private void ExecuteAndVerify(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Executing);
        }

        private void ExecuteAndNotify(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Executing);
            for (int i = 0; i < DeterminateLoops; i++)
            {
                notifier.ProgressChanged(i.ToString(), (double)(i + 1) / (double)DeterminateLoops);
            }
        }

        private void ExecuteAndFail(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Executing);
            throw new Exception("Boom");
        }

        private void ExecuteAndCancell(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Executing);
            this.testController.IsCurrentStepCancellable.Should().BeTrue("Expected to be cancellable");
            this.testController.Cancel();
            token.ThrowIfCancellationRequested();
        }

        private void ExecuteNonCancellable(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            VerificationHelper.CheckState(this.testSubject, StepExecutionState.Executing);
            this.testController.IsCurrentStepCancellable.Should().BeFalse("Not expected to be cancellable");
        }

        #endregion
    }
}
