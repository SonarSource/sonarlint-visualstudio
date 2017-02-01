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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class SequentialProgressControllerTests
    {
        private SequentialProgressController testSubject;
        private readonly List<Tuple<string, double>> notifyProgressSequence = new List<Tuple<string, double>>();
        private ConfigurableServiceProvider serviceProvider;
        private SingleThreadedTaskSchedulerService threadingService;
        private ConfigurableErrorNotifier errorNotifier;

        #region Test plumbing

        public TestContext TestContext
        {
            get;
            set;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.threadingService = new SingleThreadedTaskSchedulerService();
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), this.threadingService);
            this.testSubject = new SequentialProgressController(this.serviceProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.errorNotifier = null;
            this.serviceProvider = null;
            this.testSubject = null;
            this.threadingService = null;
        }

        #endregion Test plumbing

        #region General tests

        [TestMethod]
        public void SequentialProgressController_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SequentialProgressController(null));

            // 2 Arguments overload
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.Initialize((IProgressStepFactory)null, stepsDefinition: null));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.Initialize(new ConfigurableProgressStepFactory(), stepsDefinition: null));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.Initialize((IProgressStepFactory)null, stepsDefinition: new IProgressStepDefinition[0]));

            // 1 Argument overload
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.Initialize(stepsDefinition: null));
        }

        [TestMethod]
        public void SequentialProgressController_EventRegistration_NonUIThread()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(false);

            Exceptions.Expect<COMException>(() => this.testSubject.CancellationSupportChanged += (o, e) => { });
            Exceptions.Expect<COMException>(() => this.testSubject.Finished += (o, e) => { });
            Exceptions.Expect<COMException>(() => this.testSubject.Started += (o, e) => { });
            Exceptions.Expect<COMException>(() => this.testSubject.StepExecutionChanged += (o, e) => { });
        }

        [TestMethod]
        [Description("Verifies execution order")]
        public void SequentialProgressController_ExecutionOrder()
        {
            // Arrange
            IProgressStep[] stepOperations = null;
            int expectedOperation = 0;
            Action<CancellationToken, IProgressStepExecutionEvents> operation = (c, e) =>
                {
                    ((IProgressStep)e).Should().Be(stepOperations[expectedOperation], "Unexpected execution order");
                    expectedOperation++;
                };

            ProgressStepDefinition[] definitions = new[]
            {
                new ProgressStepDefinition(null, StepAttributes.None, operation),
                new ProgressStepDefinition(null, StepAttributes.Hidden, operation),
                new ProgressStepDefinition(null, StepAttributes.BackgroundThread, operation),
                new ProgressStepDefinition(null, StepAttributes.Indeterminate, operation),
                new ProgressStepDefinition(null, StepAttributes.NonCancellable, operation)
            };

            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(definitions);
            stepOperations = this.testSubject.Steps.ToArray();

            // Act
            this.testSubject.Start().Wait();

            // Assert
            definitions.Length.Should().Be(expectedOperation, "Executed unexpected number of times");
        }

        [TestMethod]
        [Description("Verifies that the controller can be executed from the UI thread")]
        public void SequentialProgressController_Execution_UIThread()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(true);
            this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.None, this.DoNothing));

            // Act
            this.testSubject.Start().Result.Should().Be(ProgressControllerResult.Succeeded, "Unexpected result");
        }

        [TestMethod]
        [Description("Verifies that the controller can be executed from a non-UI thread")]
        public void SequentialProgressController_Execution_NonUIThread()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(false);
            this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.None, this.DoNothing));

            // Act
            this.testSubject.Start().Result.Should().Be(ProgressControllerResult.Succeeded, "Unexpected result");
        }

        #endregion General tests

        #region IProgressController implementation tests

        [TestMethod]
        [Description("Verifies that in case Initialized is called twice an exception will be thrown")]
        public void SequentialProgressController_IProgressController_Initialize_Twice()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            this.testSubject.Initialize(testFactory, new IProgressStepDefinition[0]);

            // Act and verify
            Exceptions.Expect<InvalidOperationException>(() => this.testSubject.Initialize(testFactory, new IProgressStepDefinition[0]));
        }

        [TestMethod]
        [Description("Verifies that in case Start is called twice an exception will be thrown")]
        public void SequentialProgressController_IProgressController_Start_Twice()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            this.testSubject.Initialize(testFactory, new IProgressStepDefinition[0]);

            // Act in parallel and verify
            Exceptions.Expect<InvalidOperationException>(() =>
                {
                    try
                    {
                        Task.WaitAll(
                            this.testSubject.Start(),
                            this.testSubject.Start());
                    }
                    catch (AggregateException ex)
                    {
                        throw ex.InnerException;
                    }
                });
        }

        [TestMethod]
        [Description("Verifies that Start will change IsStarted and IsFinished during execution")]
        public void SequentialProgressController_IProgressController_Start()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            ConfigurableProgressTestOperation stepOperation = new ConfigurableProgressTestOperation(this.VerifyControllerExecuting);
            testFactory.CreateOpeartion = (d) => stepOperation;
            this.testSubject.Initialize(testFactory, new[] { new StubProgressStepDefinition() });

            this.testSubject.IsStarted.Should().BeFalse("Wasn't started yet");
            this.testSubject.IsFinished.Should().BeFalse("Wasn't started yet");

            // Act
            this.testSubject.Start().Wait();

            this.testSubject.IsStarted.Should().BeTrue("Was started");
            this.testSubject.IsFinished.Should().BeTrue("Was finished");
        }

        [TestMethod]
        [Description("Verifies the initialize method - that uses the factory correctly to convert definitions to operations")]
        public void SequentialProgressController_IProgressController_Initialize()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            IProgressStepDefinition[] definitions = new IProgressStepDefinition[]
            {
                    new StubProgressStepDefinition(),
                    new StubProgressStepDefinition(),
                    new StubProgressStepDefinition()
            };

            // Act
            this.testSubject.Initialize(testFactory, definitions);

            // Assert
            IProgressStepOperation[] stepOperations = this.testSubject.Steps.OfType<IProgressStepOperation>().ToArray();
            testFactory.AssertStepOperationsCreatedForDefinitions(definitions, stepOperations);
        }

        [TestMethod]
        [Description("Verifies that using TryAbort before the controller has started will not be possible")]
        public void SequentialProgressController_IProgressController_TryAbort_NonStarted()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(
                new ProgressStepDefinition(null, StepAttributes.Hidden | StepAttributes.NonCancellable, this.DoNothing),
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));
            this.testSubject.TryAbort().Should().BeFalse("Should not be able to abort before started");

            // Act
            this.testSubject.Start().Wait();

            // Assert
            IProgressStep[] stepOperations = this.testSubject.Steps.ToArray();
            verifier.AssertCorrectExecution(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[1], StepExecutionState.Succeeded);
            verifier.AssertCancellationChanges(3);
        }

        [TestMethod]
        [Description("Verifies that using TryAbort on a non cancellable step will not be possible")]
        public void SequentialProgressController_IProgressController_TryAbort_NonCancellable()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(
                new ProgressStepDefinition(null, StepAttributes.Hidden | StepAttributes.NonCancellable, this.RequestToCancelIgnored),
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            IProgressStep[] stepOperations = this.testSubject.Steps.ToArray();
            verifier.AssertCorrectExecution(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[1], StepExecutionState.Succeeded);
            verifier.AssertCancellationChanges(3);
        }

        [TestMethod]
        [Description("Verifies that using TryAbort on a cancellable step will be possible")]
        public void SequentialProgressController_IProgressController_TryAbort_Cancellable()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.RequestToCancelAccepted),
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            this.testSubject.CanAbort.Should().BeFalse("Should not be abortable any more, since already aborted");
            IProgressStep[] stepOperations = this.testSubject.Steps.ToArray();
            verifier.AssertCorrectExecution(ProgressControllerResult.Cancelled);
            verifier.AssertStepCorrectExecution(stepOperations[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[1], StepExecutionState.NotStarted);
            verifier.AssertCancellationChanges(2);
        }

        [TestMethod]
        [Description("Verifies that in case the will not cancel itself using the cancellation token, the controller will still cancel before running the next step")]
        public void SequentialProgressController_IProgressController_TryAbort_ControllerDrivenCancellation()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            ConfigurableProgressTestOperation stepOperation = new ConfigurableProgressTestOperation(this.DoNothing);
            stepOperation.CancellableAction = () =>
            {
                // Using this opportunity to abort - the test assumes that Cancellable is called before the step is actually executed
                this.testSubject.TryAbort().Should().BeTrue("Should be able to abort");
                return true;
            };
            testFactory.CreateOpeartion = (d) => stepOperation;
            ProgressEventsVerifier verifier = new ProgressEventsVerifier(this.testSubject);
            this.testSubject.Initialize(testFactory, new[] { new StubProgressStepDefinition() });

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Cancelled);
            verifier.AssertStepCorrectExecution(stepOperation, StepExecutionState.NotStarted);
            verifier.AssertCancellationChanges(3);
        }

        #endregion IProgressController implementation tests

        #region IProgressEvents implementation tests

        [TestMethod]
        [Description("Verifies that step execution event for succeeded step is raised correctly")]
        public void SequentialProgressController_IProgressEvents_StepSucceeded()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Succeeded);
            verifier.AssertCancellationChanges(1);
        }

        [TestMethod]
        [Description("Verifies that step execution event for canceled step is raised correctly")]
        public void SequentialProgressController_IProgressEvents_StepCancelled()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.Cancel));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Cancelled);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Cancelled);
            verifier.AssertCancellationChanges(2);
        }

        [TestMethod]
        [Description("Verifies that step execution event for failed step is raised correctly")]
        public void SequentialProgressController_IProgressEvents_StepFailed()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.Fail));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Failed);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Failed);
            verifier.AssertCancellationChanges(1);
            this.errorNotifier.Exceptions.Should().HaveCount(1);
        }

        [TestMethod]
        [Description("Verifies that step execution events for multiple succeeded steps are raised correctly")]
        public void SequentialProgressController_IProgressEvents_MultiStep_Succeeded()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Succeeded);
            IProgressStep[] step = this.testSubject.Steps.ToArray();
            verifier.AssertStepCorrectExecution(step[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(step[1], StepExecutionState.Succeeded);
            verifier.AssertCancellationChanges(1);
        }

        [TestMethod]
        [Description("Verifies that step execution events for multiple steps with a single canceled one are raised correctly")]
        public void SequentialProgressController_IProgressEvents_MultiStep_Cancelled()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.Cancel),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Cancelled);
            IProgressStep[] step = this.testSubject.Steps.ToArray();
            verifier.AssertStepCorrectExecution(step[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(step[1], StepExecutionState.Cancelled);
            verifier.AssertStepCorrectExecution(step[2], StepExecutionState.NotStarted);
            verifier.AssertCancellationChanges(2);
        }

        [TestMethod]
        [Description("Verifies that step execution events for multiple steps with a single failed one are raised correctly")]
        public void SequentialProgressController_IProgressEvents_MultiStep_Failed()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.Fail),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Failed);
            IProgressStep[] step = this.testSubject.Steps.ToArray();
            verifier.AssertStepCorrectExecution(step[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(step[1], StepExecutionState.Failed);
            verifier.AssertStepCorrectExecution(step[2], StepExecutionState.NotStarted);
            verifier.AssertCancellationChanges(1);
        }

        [TestMethod]
        [Description("Verifies that step execution progress updates are raised correctly")]
        public void SequentialProgressController_IProgressEvents_ProgessChanges()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.NotifyProgress));
            this.notifyProgressSequence.Add(Tuple.Create("hello", 0.25));
            this.notifyProgressSequence.Add(Tuple.Create(string.Empty, 0.5));
            this.notifyProgressSequence.Add(Tuple.Create("world", 0.75));
            this.notifyProgressSequence.Add(Tuple.Create((string)null, 1.0));

            // Act
            this.testSubject.Start().Wait();

            // Assert
            verifier.AssertCorrectExecution(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Succeeded);
            verifier.AssertExecutionProgress(this.testSubject.Steps.Single(), this.notifyProgressSequence.ToArray());
            verifier.AssertCancellationChanges(1);
        }

        #endregion IProgressEvents implementation tests

        #region Test helpers

        private static void AssertOperationArgumentsAreNotNull(CancellationToken token, IProgressStepExecutionEvents callback)
        {
            token.Should().NotBeNull("CancellationToken is expected not to be null");
            callback.Should().NotBeNull("IProgressStepExecutionEvents is expected not to be null");
        }

        private ProgressEventsVerifier InitializeTestSubjectWithTestErrorHandling(params ProgressStepDefinition[] definitions)
        {
            // Replace the error handler
            this.errorNotifier = SequentialProgressControllerHelper.InitializeWithTestErrorHandling(this.testSubject, definitions);
            ProgressEventsVerifier verifier = null;
            this.threadingService.RunInUIContext(() => verifier = new ProgressEventsVerifier(this.testSubject));
            return verifier;
        }

        /// <summary>
        /// Step operation that doesn't do anything
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="notifier">Progress notifier</param>
        private void DoNothing(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            AssertOperationArgumentsAreNotNull(token, notifier);
        }

        /// <summary>
        /// Step operation that verifies that the controller is started but not finished
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="notifier">Progress notifier</param>
        private void VerifyControllerExecuting(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            AssertOperationArgumentsAreNotNull(token, notifier);

            this.testSubject.IsStarted.Should().BeTrue("Expected to be started");
            this.testSubject.IsFinished.Should().BeFalse("Not expected to be finished");
        }

        /// <summary>
        /// Step operation that will abort and throw cancellation exception
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="notifier">Progress notifier</param>
        private void Cancel(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            AssertOperationArgumentsAreNotNull(token, notifier);

            this.testSubject.TryAbort().Should().BeTrue("Expected to abort");
            token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Step operation that will request to cancel in a cancellable step
        /// </summary>
        /// <remarks>Simulates an attempt to cancel during cancellable step execution</remarks>
        /// <param name="token">Cancellation token</param>
        /// <param name="notifier">Progress notifier</param>
        private void RequestToCancelAccepted(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            AssertOperationArgumentsAreNotNull(token, notifier);

            this.testSubject.CanAbort.Should().BeTrue("Should be able to abort");
            this.testSubject.TryAbort().Should().BeTrue("Aborting should succeed");
        }

        /// <summary>
        /// Step operation that will request to cancel but the step is cannot be canceled
        /// </summary>
        /// <remarks>Simulates an attempt to cancel during non-cancellable step execution</remarks>
        /// <param name="token">Cancellation token</param>
        /// <param name="notifier">Progress notifier</param>
        private void RequestToCancelIgnored(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            AssertOperationArgumentsAreNotNull(token, notifier);

            this.testSubject.CanAbort.Should().BeFalse("Should not be able to abort");
            this.testSubject.TryAbort().Should().BeFalse("Aborting should fail");
        }

        /// <summary>
        /// Step operation that will throw an exception and cause the step to fail
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="notifier">Progress notifier</param>
        private void Fail(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            AssertOperationArgumentsAreNotNull(token, notifier);

            throw new Exception();
        }

        /// <summary>
        /// Step operation that will notify the progress
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="notifier">Progress notifier</param>
        private void NotifyProgress(CancellationToken token, IProgressStepExecutionEvents notifier)
        {
            AssertOperationArgumentsAreNotNull(token, notifier);

            this.notifyProgressSequence.ForEach(t => notifier.ProgressChanged(t.Item1, t.Item2));
        }

        #endregion Test helpers
    }
}