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
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;

using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.MVVM;
using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    public class ProgressObserverTests
    {
        #region Fields
        private const double FloatingPointError = 0.00001;

        private ProgressObserver testSubject;
        private ConfigurableProgressVisualizer testVisualizer;
        private ConfigurableServiceProvider testServiceProvider;
        private ConfigurableProgressEvents progressEvents;
        private ConfigurableProgressController testController;
        private SingleThreadedTaskSchedulerService threadService;
        #endregion

        public ProgressObserverTests()
        {
            this.testServiceProvider = new ConfigurableServiceProvider();
            this.testVisualizer = new ConfigurableProgressVisualizer();
            this.progressEvents = new ConfigurableProgressEvents();
            this.testController = new ConfigurableProgressController(this.testServiceProvider);
            this.testController.Events = this.progressEvents;
            this.threadService = new SingleThreadedTaskSchedulerService();
            this.testServiceProvider.RegisterService(typeof(SVsTaskSchedulerService), this.threadService);
        }

        #region Static methods tests
        [Fact]
        public void StartObserving_WithNullProgressVisualizer_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => ProgressObserver.StartObserving(this.testController, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void StartObserving_WithNullProgressController_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => ProgressObserver.StartObserving(null, this.testVisualizer);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void StartObserving_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => ProgressObserver.StartObserving(null, this.progressEvents, this.testVisualizer);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        public void StartObserving_WithProgressControllerWithNullEvents_ThrowsArgumentNullException()
        {
            // Arrange & Act
            this.testController.Events = null;
            Action act = () => ProgressObserver.StartObserving(this.testController, this.testVisualizer);

            // Assert
            act.ShouldThrow<InvalidOperationException>();
        }
        public void StartObserving_WithProgressControllerWithNullEventsSteps_ThrowsArgumentNullException()
        {
            // Arrange & Act
            this.testController.Events.Steps = null;
            Action act = () => ProgressObserver.StartObserving(this.testController, this.testVisualizer);

            // Assert
            act.ShouldThrow<InvalidOperationException>();
        }


        [Fact]
        public void StartObserving_WithThreeArgsAndNullProgressEvents_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => ProgressObserver.StartObserving(this.testServiceProvider, null, this.testVisualizer);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void StartObserving_WithThreeArgsAndNullProgressVisualizer_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => ProgressObserver.StartObserving(this.testServiceProvider, this.progressEvents, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void StartObserving_WithThreeArgsAndInvalidProgressEvents_ThrowsInvalidOperationExceptionException()
        {
            // Arrange & Act
            Action act = () => ProgressObserver.StartObserving(this.testServiceProvider, this.progressEvents, this.testVisualizer);

            // Assert
            act.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void StopObserving_WithNullProgressObserver_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => ProgressObserver.StopObserving(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]

        public void ProgressObserver_StartAndStopObserving_OnForegroundThread()
        {
            this.threadService.SetCurrentThreadIsUIThread(true);

            this.testSubject = ProgressObserver.StartObserving(this.testController, this.testVisualizer);
            this.testSubject.Should().NotBeNull("Failed to create observer on a foreground thread");
            this.testSubject.Visualizer.Should().BeSameAs(this.testVisualizer, "Unexpected visualizer");

            this.testSubject.IsDisposed.Should().BeFalse("Not expecting to be disposed");
            ProgressObserver.StopObserving(this.testSubject);
            this.testSubject.Visualizer.Should().BeSameAs(this.testVisualizer, "Unexpected visualizer");
            this.testSubject.IsDisposed.Should().BeTrue("Expecting to be disposed");
        }

        [Fact]

        public void ProgressObserver_StartObserving_StateTransfer()
        {
            var observer = ProgressObserver.StartObserving(this.testController, this.testVisualizer);
            ProgressObserver.StopObserving(observer);
            this.CreateTestSubject(observer.State);

            observer.Should().NotBeSameAs(this.testSubject.State, "Test setup error: should be different");
            observer.State.Should().BeSameAs(this.testSubject.State, "The state should transfer");
            this.testSubject.IsDisposed.Should().BeFalse("Not expecting to be disposed");

            ProgressObserver.StopObserving(this.testSubject);
            this.testSubject.Visualizer.Should().BeSameAs(this.testVisualizer, "Unexpected visualizer");
            this.testSubject.IsDisposed.Should().BeTrue("Expecting to be disposed");
        }

        [Fact]

        public void ProgressObserver_StartAndStopObserving_OnBackgroundThread()
        {
            this.threadService.SetCurrentThreadIsUIThread(false);

            this.testSubject = ProgressObserver.StartObserving(this.testController, this.testVisualizer);
            this.testSubject.Should().NotBeNull("Failed to create observer on a background thread");
            this.testSubject.Visualizer.Should().BeSameAs(this.testVisualizer, "Unexpected visualizer");

            this.testSubject.IsDisposed.Should().BeFalse("Not expecting to be disposed");
            ProgressObserver.StopObserving(this.testSubject);
            this.testSubject.Visualizer.Should().BeSameAs(this.testVisualizer, "Unexpected visualizer");
            this.testSubject.IsDisposed.Should().BeTrue("Expecting to be disposed");
        }

        [Fact]

        public void ProgressObserver_StopObserving_Twice()
        {
            this.testSubject = ProgressObserver.StartObserving(this.testController, this.testVisualizer);

            ProgressObserver.StopObserving(this.testSubject);
            this.testSubject.IsDisposed.Should().BeTrue("Expecting to be disposed");

            ProgressObserver.StopObserving(this.testSubject);
            this.testSubject.IsDisposed.Should().BeTrue("Expecting to remain disposed");
        }

        [Fact]

        public void ProgressObserver_StartObserving_ConfiguresCancelCommand()
        {
            bool aborted = false;
            this.testController.TryAbortAction = () =>
            {
                aborted = true;
                return true;
            };

            this.testSubject = ProgressObserver.StartObserving(this.testController, this.testVisualizer);
            this.testSubject.CancelCommand.Should().NotBeNull("CancelCommand should be set");
            this.testSubject.CancelCommand.Execute(null);
            aborted.Should().BeTrue("TryAbort was configured to be called by the CancelCommand");
        }

        [Fact]

        public void ProgressObserver_StartObserving_DontConfiguresCancelCommand()
        {
            // Execute
            this.testSubject = ProgressObserver.StartObserving(this.testServiceProvider, this.progressEvents, this.testVisualizer);

            // Assert
            this.testSubject.CancelCommand.Should().BeNull("CancelCommand should not be set");
        }

        [Fact]

        public void ProgressObserver_GroupToExecutionUnits()
        {
            ProgressObserver.ExecutionGroup[] result;

            // Visible Hidden Visible
            bool randomIndeterminate = Environment.TickCount % 2 == 0;
            var visibleHiddenVisible = CreateRandomSteps(1, true, randomIndeterminate, true)
                .Union(CreateRandomSteps(1, false, randomIndeterminate, true))
                .Union(CreateRandomSteps(1, true, randomIndeterminate, true)).ToArray();
            result = ProgressObserver.GroupToExecutionUnits(visibleHiddenVisible);
            result.Should().HaveCount(2, "Unexpected number of groups");
            VerifyExecutionGroup(result[0], visibleHiddenVisible.Take(2));
            VerifyExecutionGroup(result[1], new[] { visibleHiddenVisible[2] });

            // Hidden Visible Hidden
            randomIndeterminate = Environment.TickCount % 2 == 0;
            var hiddenVisibleHidden = CreateRandomSteps(1, false, randomIndeterminate, true)
                .Union(CreateRandomSteps(1, true, randomIndeterminate, true))
                .Union(CreateRandomSteps(1, false, randomIndeterminate, true));
            result = ProgressObserver.GroupToExecutionUnits(hiddenVisibleHidden);
            result.Should().HaveCount(1, "Unexpected number of groups");
            VerifyExecutionGroup(result[0], hiddenVisibleHidden);

            // All visible
            randomIndeterminate = Environment.TickCount % 2 == 0;
            var allVisible = CreateRandomSteps(3, true, randomIndeterminate, true).ToArray();
            result = ProgressObserver.GroupToExecutionUnits(allVisible);
            result.Should().HaveCount(3, "Unexpected number of groups");
            VerifyExecutionGroup(result[0], new[] { allVisible[0] });
            VerifyExecutionGroup(result[1], new[] { allVisible[1] });
            VerifyExecutionGroup(result[2], new[] { allVisible[2] });

            // All hidden
            randomIndeterminate = Environment.TickCount % 2 == 0;
            var allHidden = CreateRandomSteps(3, false, randomIndeterminate, true);
            result = ProgressObserver.GroupToExecutionUnits(allHidden);
            result.Should().BeEmpty("Unexpected number of groups");
        }

        #endregion

        #region Instance method tests
        [Fact]

        public void ProgressObserver_InitializationAndCleanup()
        {
            // Arrange
            this.CreateTestSubject();

            // Invoke finished
            this.progressEvents.InvokeFinished(ProgressControllerResult.Failed /*doesn't matter*/);

            // Assert state
            this.VerifyDisposedAndUnregistered();
        }

        [Fact]

        public void ProgressObserver_DisplayTitle()
        {
            // Arrange
            this.CreateTestSubject();

            // Assert state
            this.testSubject.DisplayTitle.Should().BeNull("DisplayTitle should be null be default");
            this.VerifyControllerAndViewModelPropertiesMatch();

            // Set property
            string propertyValue = Environment.TickCount.ToString();
            this.testSubject.DisplayTitle = propertyValue;
            this.testSubject.DisplayTitle.Should().Be(propertyValue, "DisplayTitle is not set as expected");
            this.VerifyControllerAndViewModelPropertiesMatch();
        }

        [Fact]

        public void ProgressObserver_CancelCommand()
        {
            // Arrange
            this.CreateTestSubject();

            // Assert state
            this.testSubject.CancelCommand.Should().BeNull("CancelCommand should be null be default");
            this.VerifyControllerAndViewModelPropertiesMatch();

            // Set property
            ICommand propertyValue = new RelayCommand(s => { });
            this.testSubject.CancelCommand = propertyValue;
            this.testSubject.CancelCommand.Should().BeSameAs(propertyValue, "CancelCommand is not set as expected");
            this.VerifyControllerAndViewModelPropertiesMatch();
        }

        [Fact]

        public void ProgressObserver_ViewModelSteps_AllVisibleSteps_ImpactingProgress()
        {
            // Arrange
            IProgressStep[] steps = CreateRandomSteps(5, true, false, true);
            this.progressEvents.Steps = steps;
            this.CreateTestSubject();

            // Assert
            this.VerifySteps(5, 0);
        }

        [Fact]

        public void ProgressObserver_ViewModelSteps_AllVisibleSteps_NotImpactingProgress()
        {
            // Arrange
            IProgressStep[] steps = CreateRandomSteps(5, true, false, false);
            this.progressEvents.Steps = steps;
            this.CreateTestSubject();

            // Assert
            this.VerifySteps(0, 0);
        }

        [Fact]

        public void ProgressObserver_ViewModelSteps_VisibleAndHiddenSteps_ImpactingProgress()
        {
            // Arrange
            IProgressStep[] visible = CreateRandomSteps(5, true, true, true);
            IProgressStep[] hidden = CreateRandomSteps(2, false, false, true);
            this.progressEvents.Steps = visible.Union(hidden);
            this.CreateTestSubject();

            // Assert
            this.VerifySteps(5, 2);
        }

        [Fact]

        public void ProgressObserver_ViewModelSteps_VisibleAndHiddenSteps_NotImpactingProgress()
        {
            // Arrange
            IProgressStep[] visible = CreateRandomSteps(5, true, true, false);
            IProgressStep[] hidden = CreateRandomSteps(2, false, false, false);
            this.progressEvents.Steps = visible.Union(hidden);
            this.CreateTestSubject();

            // Assert
            this.VerifySteps(0, 0);
        }

        [Fact]

        public void ProgressObserver_EventMonitoringAndExecution_ViewModelOutOfSync()
        {
            ConfigurableProgressTestOperation step = new ConfigurableProgressTestOperation((c, e) => { });
            step.Progress = 0.0;
            step.Indeterminate = false;
            step.ExecutionState = StepExecutionState.NotStarted;
            this.progressEvents.Steps = new IProgressStep[] { step };
            this.CreateTestSubject();

            // Create another step which is not observed
            ConfigurableProgressTestOperation anotherStep = new ConfigurableProgressTestOperation((c, e) => { });
            anotherStep.ExecutionState = StepExecutionState.Succeeded;
            anotherStep.Progress = 1.0;
            anotherStep.Indeterminate = false;

            using (new AssertIgnoreScope())
            {
                this.progressEvents.InvokeStepExecutionChanged(new StepExecutionChangedEventArgs(anotherStep));
            }

            this.testVisualizer.Root.MainProgress.Value.Should().Be(0.0, "The main progress should not change");
        }

        [Fact]

        public void ProgressObserver_EventMonitoringAndExecution()
        {
            // Arrange - Create a determinate not started step
            int initialProgress = 0;
            string initialProgressDetails = null;
            ConfigurableProgressTestOperation step = new ConfigurableProgressTestOperation((c, e) => { });
            step.Progress = initialProgress;
            step.ProgressDetailText = initialProgressDetails;
            step.Indeterminate = false;
            step.ExecutionState = StepExecutionState.NotStarted;
            step.ImpactsProgress = true;
            this.progressEvents.Steps = new IProgressStep[] { step };
            this.CreateTestSubject();
            this.threadService.SetCurrentThreadIsUIThread(false);

            // Show
            this.testVisualizer.AssertIsHidden();
            this.testSubject.IsFinished.Should().BeFalse("Not started");
            this.progressEvents.InvokeStarted();
            this.testSubject.IsFinished.Should().BeFalse("Just started");
            this.testVisualizer.AssertIsShown();

            // Cancellability change
            this.progressEvents.InvokeCancellationSupportChanged(false);
            this.testVisualizer.Root.Cancellable.Should().BeFalse("Unexpected cancellable state");
            this.progressEvents.InvokeCancellationSupportChanged(true);
            this.testVisualizer.Root.Cancellable.Should().BeTrue("Unexpected cancellable state");

            ProgressStepViewModel viewModelStep = this.testVisualizer.Root.Steps[0];

            // Step execution changed
            viewModelStep.ExecutionState.Should().Be(StepExecutionState.NotStarted, "Inconclusive: unexpected initial state");
            step.ExecutionState = StepExecutionState.Executing;
            this.progressEvents.InvokeStepExecutionChanged(new StepExecutionChangedEventArgs(step));
            viewModelStep.ExecutionState.Should().Be(StepExecutionState.Executing, "Execution state wasn't changed as expected");

            // Step progress reporting
            step.ProgressDetailText = "Hello world";
            step.Progress = 1.0;
            viewModelStep.Progress.Value.Should().Be(initialProgress, "Inconclusive: unexpected initial Progress");
            viewModelStep.ProgressDetailText.Should().Be(initialProgressDetails, "Inconclusive: unexpected initial ProgressDetailText");
            this.progressEvents.InvokeStepExecutionChanged(new StepExecutionChangedEventArgs(step));
            viewModelStep.Progress.Value.Should().Be(step.Progress, "Progress wasn't changed as expected");
            viewModelStep.ProgressDetailText.Should().Be(step.ProgressDetailText, "ProgressDetailText wasn't changed as expected");

            // Hide
            this.testSubject.IsFinished.Should().BeFalse("Not done yet");
            this.progressEvents.InvokeFinished(ProgressControllerResult.Cancelled/*doesn't matter*/);
            this.testSubject.IsFinished.Should().BeTrue("Can celled - > Finished");
            this.testVisualizer.AssertIsHidden();
        }

        [Fact]

        public void ProgressObserver_ProgressUpdate_DeterminateIndeterminate()
        {
            // Arrange
            ConfigurableProgressTestOperation determinate1 = CreateRandomStep(visible: true, indeterminate: false, impacting: true);
            determinate1.Progress = 0;
            determinate1.ExecutionState = StepExecutionState.NotStarted;

            ConfigurableProgressTestOperation indeterminate1 = CreateRandomStep(visible: true, indeterminate: true, impacting: true);
            indeterminate1.Progress = ProgressControllerHelper.Indeterminate;
            indeterminate1.ExecutionState = StepExecutionState.NotStarted;

            ConfigurableProgressTestOperation determinate2 = CreateRandomStep(visible: true, indeterminate: false, impacting: true);
            determinate2.Progress = 0;
            determinate2.ExecutionState = StepExecutionState.NotStarted;

            ConfigurableProgressTestOperation indeterminate2 = CreateRandomStep(visible: true, indeterminate: true, impacting: true);
            indeterminate2.Progress = ProgressControllerHelper.Indeterminate;
            indeterminate2.ExecutionState = StepExecutionState.NotStarted;

            this.progressEvents.Steps = new[] { determinate1, indeterminate1, determinate2, indeterminate2 };
            this.CreateTestSubject();
            double mainProgressSections = this.progressEvents.Steps.Count(s => s.ImpactsProgress);

            // Assert initial state
            VerifyProgress(this.testVisualizer, 0, null, 0);

            ExecutionVerifier verifier = new ExecutionVerifier(this.testVisualizer, this.testSubject);
            verifier.AppendStepToGroup(0, determinate1);
            verifier.AppendStepToGroup(1, indeterminate1);
            verifier.AppendStepToGroup(2, determinate2);
            verifier.AppendStepToGroup(3, indeterminate2);

            // First started to execute
            determinate1.ExecutionState = StepExecutionState.Executing;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, determinate1, 0);

            // First reports progress
            determinate1.Progress = 0.5;
            verifier.ExpectedSubProgress = 0.5;
            verifier.ExpectedMainProgress = 0.5 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, determinate1, 0);

            // First completes
            determinate1.ExecutionState = StepExecutionState.Succeeded;
            verifier.ExpectedSubProgress = 1.0;
            verifier.ExpectedMainProgress = 1.0 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, determinate1, 0);

            // Second starts
            indeterminate1.ExecutionState = StepExecutionState.Executing;
            verifier.ExpectedSubProgress = ProgressControllerHelper.Indeterminate;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, indeterminate1, 1);

            // Second completes
            indeterminate1.ExecutionState = StepExecutionState.Succeeded;
            verifier.ExpectedSubProgress = ProgressControllerHelper.Indeterminate;
            verifier.ExpectedMainProgress = 2.0 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, indeterminate1, 1);

            // Third starts
            determinate2.ExecutionState = StepExecutionState.Executing;
            verifier.ExpectedSubProgress = 0.0;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, determinate2, 2);

            // Third reports progress
            determinate2.Progress = 0.5;
            verifier.ExpectedSubProgress = 0.5;
            verifier.ExpectedMainProgress = 2.5 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, determinate2, 2);

            // Third reports progress
            determinate2.Progress = 1.0;
            verifier.ExpectedSubProgress = 1.0;
            verifier.ExpectedMainProgress = 3.0 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, determinate2, 2);

            // Third completes
            determinate2.ExecutionState = StepExecutionState.Succeeded;
            verifier.ExpectedSubProgress = 1.0;
            verifier.ExpectedMainProgress = 3.0 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, determinate2, 2);

            // Fourth completes
            indeterminate2.ExecutionState = StepExecutionState.Succeeded;
            verifier.ExpectedSubProgress = ProgressControllerHelper.Indeterminate;
            verifier.ExpectedMainProgress = 4.0 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, indeterminate2, 3);
        }

        [Fact]

        public void ProgressObserver_ProgressUpdate_VisibleHiddenNotImpacting()
        {
            // Arrange
            bool randomIndeterminate = Environment.TickCount % 2 == 0;
            ConfigurableProgressTestOperation noImpacting1 = CreateRandomStep(visible: true, indeterminate: randomIndeterminate, impacting: false);
            noImpacting1.ExecutionState = StepExecutionState.NotStarted;
            noImpacting1.Progress = 0;

            ConfigurableProgressTestOperation hidden1 = CreateRandomStep(visible: false, indeterminate: false, impacting: true);
            hidden1.ExecutionState = StepExecutionState.NotStarted;
            hidden1.Progress = 0;

            ConfigurableProgressTestOperation noImpacting2 = CreateRandomStep(visible: true, indeterminate: randomIndeterminate, impacting: false);
            noImpacting2.ExecutionState = StepExecutionState.NotStarted;
            noImpacting2.Progress = 0;

            ConfigurableProgressTestOperation visible1 = CreateRandomStep(visible: true, indeterminate: false, impacting: true);
            visible1.ExecutionState = StepExecutionState.NotStarted;
            visible1.Progress = 0;

            ConfigurableProgressTestOperation visible2 = CreateRandomStep(visible: true, indeterminate: false, impacting: true);
            visible2.ExecutionState = StepExecutionState.NotStarted;
            visible2.Progress = 0;

            ConfigurableProgressTestOperation hidden2 = CreateRandomStep(visible: false, indeterminate: false, impacting: true);
            hidden2.ExecutionState = StepExecutionState.NotStarted;
            hidden2.Progress = 0;

            ConfigurableProgressTestOperation[] steps = new[] { noImpacting1, hidden1, noImpacting2, visible1, visible2, hidden2 };
            this.progressEvents.Steps = steps;
            this.CreateTestSubject();
            double mainProgressSections = steps.Count(s => s.ImpactsProgress);

            // Assert initial state
            VerifyProgress(this.testVisualizer, 0, null, 0);

            ExecutionVerifier verifier = new ExecutionVerifier(this.testVisualizer, this.testSubject);
            verifier.AppendStepToGroup(0, hidden1);
            verifier.AppendStepToGroup(0, visible1);
            verifier.AppendStepToGroup(1, visible2);
            verifier.AppendStepToGroup(1, hidden2);

            // Non-impacting started to execute
            noImpacting1.Progress = 0;
            noImpacting1.ExecutionState = StepExecutionState.Executing;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, noImpacting1, null);

            // Non-impacting reports progress
            noImpacting1.Progress = 0.5;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, noImpacting1, null);

            // Non-impacting completes
            noImpacting1.Progress = 1.0;
            noImpacting1.ExecutionState = StepExecutionState.Succeeded;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, noImpacting1, null);

            // Hidden1 starts
            hidden1.ExecutionState = StepExecutionState.Executing;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, hidden1, 0);

            // Hidden1 reports progress
            hidden1.Progress = 0.5;
            verifier.ExpectedSubProgress = hidden1.Progress / 2.0; // relative to the number of sub steps in group
            verifier.ExpectedMainProgress = hidden1.Progress / mainProgressSections; // relative to the number of impacting steps
            verifier.RunAndVerifyExecutingStep(this.progressEvents, hidden1, 0);

            // Hidden1 completes
            hidden1.ExecutionState = StepExecutionState.Cancelled;
            verifier.ExpectedSubProgress = 1.0 / 2.0; // relative to the number of sub steps in group
            verifier.ExpectedMainProgress = 1.0 / mainProgressSections; // relative to the number of impacting steps
            verifier.RunAndVerifyExecutingStep(this.progressEvents, hidden1, 0);

            // Non-impacting started to execute
            noImpacting2.Progress = 0;
            noImpacting2.ExecutionState = StepExecutionState.Executing;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, noImpacting2, 0);

            // Non-impacting reports progress
            noImpacting2.Progress = 0.5;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, noImpacting2, 0);

            // Non-impacting completes
            noImpacting2.Progress = 1.0;
            noImpacting2.ExecutionState = StepExecutionState.Succeeded;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, noImpacting2, 0);

            // Visible1 starts
            visible1.ExecutionState = StepExecutionState.Executing;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, visible1, 0);

            // Visible1 reports progress
            visible1.Progress = 1.0;
            verifier.ExpectedSubProgress = 2.0 / 2.0; // relative to the number of sub steps in group
            verifier.ExpectedMainProgress = 2.0 / mainProgressSections; // relative to the number of impacting steps
            verifier.RunAndVerifyExecutingStep(this.progressEvents, visible1, 0);

            // Visible1 completes
            visible1.ExecutionState = StepExecutionState.Failed;
            verifier.ExpectedSubProgress = 1.0;
            verifier.ExpectedMainProgress = 2.0 / mainProgressSections; // relative to the number of impacting steps
            verifier.RunAndVerifyExecutingStep(this.progressEvents, visible1, 0);

            // Visible2 completes
            visible2.ExecutionState = StepExecutionState.Succeeded;
            verifier.ExpectedSubProgress = 1.0 / 2.0;  // relative to the number of sub steps in group
            verifier.ExpectedMainProgress = 3.0 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, visible2, 1);

            // Hidden2 completes
            hidden2.ExecutionState = StepExecutionState.Succeeded;
            verifier.ExpectedSubProgress = 2.0 / 2.0; // relative to the number of sub steps in group
            verifier.ExpectedMainProgress = 4.0 / mainProgressSections;
            verifier.RunAndVerifyExecutingStep(this.progressEvents, hidden2, 1);
        }

        #endregion

        #region Test helpers
        private static ConfigurableProgressTestOperation[] CreateRandomSteps(int numberOfSteps, bool visible, bool indeterminate, bool impacting)
        {
            ConfigurableProgressTestOperation[] steps = new ConfigurableProgressTestOperation[numberOfSteps];
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = CreateRandomStep(visible, indeterminate, impacting);
            }

            return steps;
        }

        private static ConfigurableProgressTestOperation CreateRandomStep(bool visible, bool indeterminate, bool impacting)
        {
            Random random = new Random();
            int maxFlag = ((int[])Enum.GetValues(typeof(StepExecutionState))).Max();
            StepExecutionState executionState = (StepExecutionState)random.Next(0, maxFlag + 1);

            ConfigurableProgressTestOperation step;
            step = new ConfigurableProgressTestOperation((c, e) => { });
            step.DisplayText = "DisplayText:" + Environment.TickCount.ToString();
            step.ExecutionState = executionState;
            step.Progress = random.NextDouble();
            step.ProgressDetailText = "ProgressDetailText:" + Environment.TickCount.ToString();
            step.Indeterminate = indeterminate;
            step.Hidden = !visible;
            step.ImpactsProgress = impacting;
            return step;
        }

        private static void VerifyStep(ProgressStepViewModel vm, IProgressStep step)
        {
            vm.DisplayText.Should().Be(step.DisplayText, "DisplayText doesn't match");
            vm.ExecutionState.Should().Be(step.ExecutionState, "ExecutionState doesn't match");
            vm.Progress.Value.Should().Be(step.Progress, "Progress doesn't match");
            vm.ProgressDetailText.Should().Be(step.ProgressDetailText, "ProgressDisplayText doesn't match");
            vm.Progress.IsIndeterminate.Should().Be(step.Indeterminate, "Indeterminate doesn't match");
        }

        private static void VerifyProgress(IProgressVisualizer visualizer, double mainProgress, ProgressStepViewModel current, double subProgress)
        {
            visualizer.ViewModel.MainProgress.Value.Should().BeApproximately(mainProgress, FloatingPointError, "Unexpected main progress");
            if (current == null)
            {
                visualizer.ViewModel.Current.Should().BeNull("Not expecting any current step");
            }
            else
            {
                visualizer.ViewModel.Current.Should().BeSameAs(current, "Unexpected current step");
                if (double.IsNaN(subProgress))
                {
                    double.IsNaN(current.Progress.Value).Should().BeTrue("Unexpected sub progress");
                }
                else
                {
                    current.Progress.Value.Should().BeApproximately(subProgress, FloatingPointError, "Unexpected sub progress");
                }
            }
        }

        private static void VerifyExecutionGroup(ProgressObserver.ExecutionGroup group, IEnumerable<IProgressStep> orderedStepsInGroup)
        {
            group.Steps.Should().HaveSameCount(orderedStepsInGroup, "Unexpected number of actual steps in group");

            int i = 0;
            foreach (IProgressStep step in orderedStepsInGroup)
            {
                group.Steps[i++].Should().BeSameAs(step, "Unexpected step in group");
            }
        }

        private void CreateTestSubject(ProgressControllerViewModel state = null)
        {
            this.testSubject = new ProgressObserver(this.testServiceProvider, this.testVisualizer, this.progressEvents, state);
            this.VerifyNonDisposedAndRegistered();
        }

        private void VerifyNonDisposedAndRegistered()
        {
            this.progressEvents.AssertAllEventsAreRegistered();
            this.testSubject.IsDisposed.Should().BeFalse("Not expected to be disposed");
        }

        private void VerifyDisposedAndUnregistered()
        {
            this.progressEvents.AssertAllEventsAreUnregistered();
            this.testSubject.IsDisposed.Should().BeTrue("Expected to be disposed");
        }

        private void VerifyControllerAndViewModelPropertiesMatch()
        {
            this.testSubject.DisplayTitle.Should().Be(this.testVisualizer.Root.Title, "View model Title and controller DisplayTitle property value don't match");
            this.testSubject.CancelCommand.Should().BeSameAs(this.testVisualizer.Root.CancelCommand, "View model and controller CancelCommand property value don't match");
        }

        private void VerifySteps(int visible, int hidden)
        {
            IProgressStep[] steps = this.progressEvents.Steps.ToArray();
            IProgressStep[] visualizedSteps = steps.Where(s => s.ImpactsProgress && !s.Hidden).ToArray();
            IProgressStep[] nonVisualizedSteps = steps.Where(s => s.ImpactsProgress && s.Hidden).ToArray();

            // Cross check the event steps
            visualizedSteps.Length.Should().Be(visible, "Inconclusive: unexpected number of visible steps");
            nonVisualizedSteps.Length.Should().Be(hidden, "Inconclusive: unexpected number of visible steps");

            // Now do the verification
            int progressReportingSteps = visualizedSteps.Length;
            this.testVisualizer.Root.Steps.Should().HaveCount(progressReportingSteps, "Unexpected number of VM steps");
            for (int i = 0; i < visualizedSteps.Length; i++)
            {
                VerifyStep(this.testVisualizer.Root.Steps[i], visualizedSteps[i]);
            }
        }

        private class ExecutionVerifier
        {
            private readonly Dictionary<int, List<IProgressStep>> groups = new Dictionary<int, List<IProgressStep>>();
            private readonly IProgressVisualizer visualizer;
            private readonly ProgressObserver testSubject;

            public ExecutionVerifier(IProgressVisualizer visualizer, ProgressObserver testSubject)
            {
                visualizer.Should().NotBeNull("IProgressVisualizer is expected");
                testSubject.Should().NotBeNull("ProgressObserver is expected");

                this.visualizer = visualizer;
                this.testSubject = testSubject;
            }

            /// <summary>
            /// The expected completion percentage for the main progress
            /// </summary>
            public double ExpectedMainProgress
            {
                get;
                set;
            }

            /// <summary>
            /// The expected sub progress
            /// </summary>
            public double ExpectedSubProgress
            {
                get;
                set;
            }

            /// <summary>
            /// The method should be used to constructed the expected groups of steps
            /// </summary>
            /// <param name="group">The target group index</param>
            /// <param name="step">The step to add to a group</param>
            public void AppendStepToGroup(int group, IProgressStep step)
            {
                List<IProgressStep> orderedSteps;
                if (!this.groups.TryGetValue(group, out orderedSteps))
                {
                    this.groups[group] = orderedSteps = new List<IProgressStep>();
                }

                orderedSteps.Add(step);
            }

            public void RunAndVerifyExecutingStep(ConfigurableProgressEvents progressEvents, IProgressStep currentStep, int? currentVmIndex)
            {
                ProgressStepViewModel currentVM = currentVmIndex.HasValue ? this.visualizer.ViewModel.Steps[currentVmIndex.Value] : null;
                bool isFinalState = ProgressControllerHelper.IsFinalState(currentStep.ExecutionState) && this.IsLastStep(currentStep);

                // Trigger the event
                progressEvents.InvokeStepExecutionChanged(new StepExecutionChangedEventArgs(currentStep));

                // Assert
                if (currentVmIndex.HasValue)
                {
                    VerifyProgress(this.visualizer, this.ExpectedMainProgress, currentVM, this.ExpectedSubProgress);

                    if (isFinalState)
                    {
                        this.testSubject.CurrentExecutingGroup.Should().BeNull("Not expecting any executing group");
                    }
                    else
                    {
                        IProgressStep[] steps = this.GetAllStepsInGroup(currentStep);
                        if (currentStep.ImpactsProgress)
                        {
                            steps.Should().NotBeNull("There should be at least one step in the group");

                            VerifyExecutionGroup(this.testSubject.CurrentExecutingGroup, steps);
                        }
                        else
                        {
                            steps.Should().BeNull("Not expecting any steps in group since not impacting, so there's no group for it");
                        }
                    }
                }
                else
                {
                    currentVM.Should().BeNull("Current VM should be null, since not impacts progress");
                    (this.testSubject.CurrentExecutingGroup == null ||
                     this.testSubject.CurrentExecutingGroup.ExecutingStep == null).Should().BeTrue("Not expecting any changes for non impacting steps");
                }
            }

            /// <summary>
            /// Returns whether the specified step is the last in group
            /// </summary>
            /// <param name="step">Step to check</param>
            /// <returns>Whether the specified step is the last in group</returns>
            private bool IsLastStep(IProgressStep step)
            {
                foreach (var keyValue in this.groups)
                {
                    if (keyValue.Value.IndexOf(step) == keyValue.Value.Count - 1)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Returns all the steps which are in the same group as the specified step
            /// </summary>
            /// <param name="step">Step to check</param>
            /// <returns>All the steps in a group with the specified step</returns>
            private IProgressStep[] GetAllStepsInGroup(IProgressStep step)
            {
                foreach (var keyValue in this.groups)
                {
                    if (keyValue.Value.Contains(step))
                    {
                        return keyValue.Value.ToArray();
                    }
                }

                return null;
            }
        }
        #endregion
    }
}
