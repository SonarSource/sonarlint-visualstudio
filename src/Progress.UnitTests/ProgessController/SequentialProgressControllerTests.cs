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

using SonarLint.VisualStudio.Progress.Controller;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{

    public class SequentialProgressControllerTests
    {
        private SequentialProgressController testSubject;
        private readonly List<Tuple<string, double>> notifyProgressSequence = new List<Tuple<string, double>>();
        private ConfigurableServiceProvider serviceProvider;
        private SingleThreadedTaskSchedulerService threadingService;
        private ConfigurableErrorNotifier errorNotifier;

        public SequentialProgressControllerTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.threadingService = new SingleThreadedTaskSchedulerService();
            this.serviceProvider.RegisterService(typeof(SVsTaskSchedulerService), this.threadingService);
            this.testSubject = new SequentialProgressController(this.serviceProvider);
        }

        #region General tests

        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SequentialProgressController(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Initialize_WithNullProgressStepFactory_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => this.testSubject.Initialize(null, stepsDefinition: null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Initialize_WithNullStepDefinition_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => this.testSubject.Initialize(new ConfigurableProgressStepFactory(), stepsDefinition: null);

                // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Initialize_WithEmptyStepDefinition_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => this.testSubject.Initialize(new ConfigurableProgressStepFactory(), stepsDefinition: new IProgressStepDefinition[0]);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CancellationSupportChanged_WhenNotOnUiThread_ThrowsComException()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(false);

            // Act
            Action act = () => this.testSubject.CancellationSupportChanged += (o, e) => { };

            // Assert
            act.ShouldThrow<COMException>();
        }

        [Fact]
        public void Finished_WhenNotOnUiThread_ThrowsComException()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(false);

            // Act
            Action act = () => this.testSubject.Finished += (o, e) => { };

            // Assert
            act.ShouldThrow<COMException>();
        }

        [Fact]
        public void Started_WhenNotOnUiThread_ThrowsComException()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(false);

            // Act
            Action act = () => this.testSubject.Started += (o, e) => { };

            // Assert
            act.ShouldThrow<COMException>();
        }

        [Fact]
        public void StepExecutionChanged_WhenNotOnUiThread_ThrowsComException()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(false);

            // Act
            Action act = () => this.testSubject.StepExecutionChanged += (o, e) => { };

            // Assert
            act.ShouldThrow<COMException>();
        }

        [Fact]
        public void SequentialProgressController_ExecutionOrder()
        {
            // Arrange
            IProgressStep[] stepOperations = null;
            int expectedOperation = 0;
            Action<CancellationToken, IProgressStepExecutionEvents> operation = (c, e) =>
                {
                    ((IProgressStep)e).Should().BeSameAs(stepOperations[expectedOperation], "Unexpected execution order");
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

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            definitions.Length.Should().Be(expectedOperation, "Executed unexpected number of times");
        }

        [Fact]
        public void SequentialProgressController_Execution_UIThread()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(true);
            this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.None, this.DoNothing));

            // Execute
            this.testSubject.Start().Result.Should().Be(ProgressControllerResult.Succeeded, "Unexpected result");
        }

        [Fact]
        public void SequentialProgressController_Execution_NonUIThread()
        {
            // Arrange
            this.threadingService.SetCurrentThreadIsUIThread(false);
            this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.None, this.DoNothing));

            // Execute
            this.testSubject.Start().Result.Should().Be(ProgressControllerResult.Succeeded, "Unexpected result");
        }

        #endregion

        #region IProgressController implementation tests
        [Fact]
        public void SequentialProgressController_IProgressController_Initialize_Twice()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            this.testSubject.Initialize(testFactory, new IProgressStepDefinition[0]);

            // Act
            Action act = () => this.testSubject.Initialize(testFactory, new IProgressStepDefinition[0]);

            // Assert
            act.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void SequentialProgressController_IProgressController_Start_Twice()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            this.testSubject.Initialize(testFactory, new IProgressStepDefinition[0]);

            // act
            Action act = () => Task.WaitAll(this.testSubject.Start(),
                                            this.testSubject.Start());

            // Arrange
            act.ShouldThrow<AggregateException>().WithInnerException<InvalidOperationException>();
        }

        [Fact]
        public void SequentialProgressController_IProgressController_Start()
        {
            // Arrange
            ConfigurableProgressStepFactory testFactory = new ConfigurableProgressStepFactory();
            ConfigurableProgressTestOperation stepOperation = new ConfigurableProgressTestOperation(this.VerifyControllerExecuting);
            testFactory.CreateOpeartion = (d) => stepOperation;
            this.testSubject.Initialize(testFactory, new[] { new StubProgressStepDefinition() });

            this.testSubject.IsStarted.Should().BeFalse("Wasn't started yet");
            this.testSubject.IsFinished.Should().BeFalse("Wasn't started yet");

            // Execute
            this.testSubject.Start().Wait();

            this.testSubject.IsStarted.Should().BeTrue("Was started");
            this.testSubject.IsFinished.Should().BeTrue("Was finished");
        }

        [Fact]
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

            // Execute
            this.testSubject.Initialize(testFactory, definitions);

            // Assert
            IProgressStepOperation[] stepOperations = this.testSubject.Steps.OfType<IProgressStepOperation>().ToArray();
            stepOperations.Should().HaveSameCount(definitions);
            for (int i = 0; i < definitions.Length; i++)
            {
                stepOperations[i].Should().Be(testFactory.CreatedOperations[definitions[i]], "Mismatch at definition {0}", i);
            }
        }

        [Fact]
        public void SequentialProgressController_IProgressController_TryAbort_NonStarted()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(
                new ProgressStepDefinition(null, StepAttributes.Hidden | StepAttributes.NonCancellable, this.DoNothing),
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));
            this.testSubject.TryAbort().Should().BeFalse("Should not be able to abort before started");

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            IProgressStep[] stepOperations = this.testSubject.Steps.ToArray();
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[1], StepExecutionState.Succeeded);
            verifier.CancellableStateChangesCount.Should().Be(3);
        }

        [Fact]
        public void SequentialProgressController_IProgressController_TryAbort_NonCancellable()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(
                new ProgressStepDefinition(null, StepAttributes.Hidden | StepAttributes.NonCancellable, this.RequestToCancelIgnored),
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            IProgressStep[] stepOperations = this.testSubject.Steps.ToArray();
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[1], StepExecutionState.Succeeded);
            verifier.CancellableStateChangesCount.Should().Be(3);
        }

        [Fact]
        public void SequentialProgressController_IProgressController_TryAbort_Cancellable()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.RequestToCancelAccepted),
                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            this.testSubject.CanAbort.Should().BeFalse("Should not be abortable any more, since already aborted");
            IProgressStep[] stepOperations = this.testSubject.Steps.ToArray();
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Cancelled);
            verifier.AssertStepCorrectExecution(stepOperations[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(stepOperations[1], StepExecutionState.NotStarted);
            verifier.CancellableStateChangesCount.Should().Be(2);
        }

        [Fact]
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

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Cancelled);
            verifier.AssertStepCorrectExecution(stepOperation, StepExecutionState.NotStarted);
            verifier.CancellableStateChangesCount.Should().Be(3);
        }
        #endregion

        #region IProgressEvents implementation tests
        [Fact]
        public void SequentialProgressController_IProgressEvents_StepSucceeded()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Succeeded);
            verifier.CancellableStateChangesCount.Should().Be(1);
        }

        [Fact]
        public void SequentialProgressController_IProgressEvents_StepCancelled()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.Cancel));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Cancelled);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Cancelled);
            verifier.CancellableStateChangesCount.Should().Be(2);
        }

        [Fact]
        public void SequentialProgressController_IProgressEvents_StepFailed()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.Fail));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Failed);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Failed);
            verifier.CancellableStateChangesCount.Should().Be(1);
            this.errorNotifier.Exceptions.Should().HaveCount(1);
        }

        [Fact]
        public void SequentialProgressController_IProgressEvents_MultiStep_Succeeded()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Succeeded);
            IProgressStep[] step = this.testSubject.Steps.ToArray();
            verifier.AssertStepCorrectExecution(step[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(step[1], StepExecutionState.Succeeded);
            verifier.CancellableStateChangesCount.Should().Be(1);
        }

        [Fact]
        public void SequentialProgressController_IProgressEvents_MultiStep_Cancelled()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.Cancel),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Cancelled);
            IProgressStep[] step = this.testSubject.Steps.ToArray();
            verifier.AssertStepCorrectExecution(step[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(step[1], StepExecutionState.Cancelled);
            verifier.AssertStepCorrectExecution(step[2], StepExecutionState.NotStarted);
            verifier.CancellableStateChangesCount.Should().Be(2);
        }

        [Fact]
        public void SequentialProgressController_IProgressEvents_MultiStep_Failed()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.Fail),
                                                new ProgressStepDefinition(null, StepAttributes.Hidden, this.DoNothing));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Failed);
            IProgressStep[] step = this.testSubject.Steps.ToArray();
            verifier.AssertStepCorrectExecution(step[0], StepExecutionState.Succeeded);
            verifier.AssertStepCorrectExecution(step[1], StepExecutionState.Failed);
            verifier.AssertStepCorrectExecution(step[2], StepExecutionState.NotStarted);
            verifier.CancellableStateChangesCount.Should().Be(1);
        }

        [Fact]
        public void SequentialProgressController_IProgressEvents_ProgessChanges()
        {
            // Arrange
            ProgressEventsVerifier verifier = this.InitializeTestSubjectWithTestErrorHandling(new ProgressStepDefinition(null, StepAttributes.Hidden, this.NotifyProgress));
            this.notifyProgressSequence.Add(Tuple.Create("hello", 0.25));
            this.notifyProgressSequence.Add(Tuple.Create(string.Empty, 0.5));
            this.notifyProgressSequence.Add(Tuple.Create("world", 0.75));
            this.notifyProgressSequence.Add(Tuple.Create((string)null, 1.0));

            // Execute
            this.testSubject.Start().Wait();

            // Assert
            verifier.IsStarted.Should().BeTrue();
            verifier.ExecutionResult.Should().Be(ProgressControllerResult.Succeeded);
            verifier.AssertStepCorrectExecution(this.testSubject.Steps.Single(), StepExecutionState.Succeeded);
            verifier.AssertExecutionProgress(this.testSubject.Steps.Single(), this.notifyProgressSequence.ToArray());
            verifier.CancellableStateChangesCount.Should().Be(1);
        }
        #endregion

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
        #endregion
    }
}
