//-----------------------------------------------------------------------
// <copyright file="ProgressObserver.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.MVVM;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Observation.View;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using SonarLint.VisualStudio.Progress.Threading;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Progress.Observation
{
    /// <summary>
    /// Observes the execution progress of <see cref="IProgressController"/> and visualizes it using the supplied <see cref="IProgressVisualizer"/>
    /// </summary>
    /// <see cref="ProgressControllerViewModel"/>
    /// <see cref="ProgressStepViewModel"/>
    /// <remarks>
    /// Any public static (Shared in Visual Basic) members of this type are thread safe. Any instance members are not guaranteed to be thread safe.
    /// Any instance members are not guaranteed to be accessible from a non-UI threads.
    /// </remarks>
    public sealed class ProgressObserver : IDisposable
    {
        #region Fields
        /// <summary>
        /// We delay closing the dialog in order to show the completed progress
        /// </summary>
        private const int DelayAfterFinishInMS = 300;

        private readonly IServiceProvider serviceProvider;
        private readonly IProgressVisualizer host;
        private readonly IProgressEvents progressEvents;
        private readonly ProgressControllerViewModel viewModelRoot;
        private readonly Dictionary<IProgressStep, ProgressStepViewModel> progressStepToViewModelMapping = new Dictionary<IProgressStep, ProgressStepViewModel>();
        private readonly Dictionary<ProgressStepViewModel, ExecutionGroup> viewModelToExecutionGroupMapping = new Dictionary<ProgressStepViewModel, ExecutionGroup>();
        private IProgressStep[] steps;
        private bool disposed;
        #endregion

        #region Constructor/destructor
        internal ProgressObserver(IServiceProvider serviceProvider, IProgressVisualizer host, IProgressEvents progressEvents, ProgressControllerViewModel state)
        {
            // Event registration must be on the UI thread
            ThreadHelper.ThrowIfNotOnUIThread();

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (progressEvents == null)
            {
                throw new ArgumentNullException(nameof(progressEvents));
            }

            Debug.Assert(progressEvents.Steps != null, "Expected to be initialized");

            this.serviceProvider = serviceProvider;
            this.host = host;
            this.progressEvents = progressEvents;

            this.progressEvents.Started += this.ControllerStarted;
            this.progressEvents.Finished += this.ControllerFinished;
            this.progressEvents.StepExecutionChanged += this.OnStepExecutionChanged;
            this.progressEvents.CancellationSupportChanged += this.OnCancellationSupportChanged;

            this.viewModelRoot = state ?? new ProgressControllerViewModel();
            this.InitializeStepViewModels();

            this.host.ViewModel = this.viewModelRoot;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ProgressObserver"/> class.
        /// </summary>
        ~ProgressObserver()
        {
            Debug.Assert(this.disposed, "ProgressObserver wasn't disposed on finished event");

            this.Dispose(false);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Returns the visualizing host of the observed progress
        /// </summary>
        public IProgressVisualizer Visualizer
        {
            get { return this.host; }
        }

        /// <summary>
        /// The display title for <see cref="ProgressControllerViewModel"/>.
        /// Can be set only from the UI thread.
        /// </summary>
        public string DisplayTitle
        {
            get
            {
                return this.viewModelRoot.Title;
            }

            set
            {
                this.ThrowIfDisposed();
                ThreadHelper.ThrowIfNotOnUIThread();

                this.viewModelRoot.Title = value;
            }
        }

        /// <summary>
        /// The cancel command for <see cref="ProgressControllerViewModel"/>.
        /// Can be set only from the UI thread.
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                return this.viewModelRoot.CancelCommand;
            }

            set
            {
                this.ThrowIfDisposed();
                ThreadHelper.ThrowIfNotOnUIThread();

                this.viewModelRoot.CancelCommand = value;
            }
        }

        /// <summary>
        /// Whether the observer has received the <see cref="IProgressEvents.Finished"/> event.
        /// Once set it will not change.
        /// </summary>
        public bool IsFinished
        {
            get;
            private set;
        }

        /// <summary>
        /// The current view state of the observer
        /// </summary>
        public ProgressControllerViewModel State
        {
            get { return this.viewModelRoot; }
        }

        internal /*for testing purposes*/ bool IsDisposed
        {
            get { return this.disposed; }
        }

        /// <summary>
        /// The current group of steps being executed
        /// </summary>
        internal /*for testing purposes*/ ExecutionGroup CurrentExecutingGroup
        {
            get;
            private set;
        }

        private IEnumerable<IProgressStep> ProgressImpactingSteps
        {
            get
            {
                Debug.Assert(this.steps != null, "Expected to be initialized");
                return this.steps.Where(s => s.ImpactsProgress);
            }
        }
        #endregion

        #region Static
        /// <summary>
        /// Starts observing <see cref="IProgressEvents"/> raised by the <see cref="IProgressController"/>
        /// and visualizes them using <see cref="WpfWindowProgressVisualizer"/>.
        /// The method is thread safe.
        /// </summary>
        /// <remarks>Will also configure the <see cref="ProgressObserver.CancelCommand"/> to trigger <see cref="IProgressController.TryAbort"/></remarks>
        /// <param name="controller">Observation subject</param>
        /// <returns>A new instance of <see cref="ProgressObserver"/> capable of observing <see cref="IProgressController"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed on finished")]
        public static ProgressObserver StartObserving(IProgressController controller)
        {
            return StartObserving(controller, new WpfWindowProgressVisualizer());
        }

        /// <summary>
        /// Starts observing <see cref="IProgressEvents"/> and visualizes them using <see cref="WpfWindowProgressVisualizer"/>.
        /// The method is thread safe.
        /// </summary>
        /// <remarks>Will not configure the <see cref="ProgressObserver.CancelCommand"/> to trigger <see cref="IProgressController.TryAbort"/></remarks>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="progressEvents"><see cref="IProgressEvents"/> to observe by</param>
        /// <returns>A new instance of <see cref="ProgressObserver"/> capable of observing <see cref="IProgressController"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed on finished")]
        public static ProgressObserver StartObserving(IServiceProvider serviceProvider, IProgressEvents progressEvents)
        {
            return StartObserving(serviceProvider, progressEvents, new WpfWindowProgressVisualizer());
        }

        /// <summary>
        /// Starts observing <see cref="IProgressEvents"/> raised by the <see cref="IProgressController"/>
        /// and visualizes them using <see cref="IProgressVisualizer"/>.
        /// The method is thread safe.
        /// </summary>
        /// <remarks>Will also configure the <see cref="ProgressObserver.CancelCommand"/> to trigger <see cref="IProgressController.TryAbort"/></remarks>
        /// <param name="controller">Observation subject</param>
        /// <param name="visualizer"><see cref="IProgressVisualizer"/> to use for visualizing the observed progress</param>
        /// <returns>A new instance of <see cref="ProgressObserver"/> capable of observing <see cref="IProgressController"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed on finished")]
        public static ProgressObserver StartObserving(IProgressController controller, IProgressVisualizer visualizer)
        {
            return StartObserving(controller, visualizer, null);
        }

        /// <summary>
        /// Starts observing <see cref="IProgressEvents"/> raised by the <see cref="IProgressController"/>
        /// and visualizes them using <see cref="IProgressVisualizer"/>.
        /// The method is thread safe.
        /// </summary>
        /// <remarks>Will also configure the <see cref="ProgressObserver.CancelCommand"/> to trigger <see cref="IProgressController.TryAbort"/></remarks>
        /// <param name="controller">Observation subject</param>
        /// <param name="visualizer"><see cref="IProgressVisualizer"/> to use for visualizing the observed progress</param>
        /// <param name="state">Optional. Pre-existing state represented by <see cref="ProgressControllerViewModel"/></param>
        /// <returns>A new instance of <see cref="ProgressObserver"/> capable of observing <see cref="IProgressController"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed on finished")]
        public static ProgressObserver StartObserving(IProgressController controller, IProgressVisualizer visualizer, ProgressControllerViewModel state)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }

            CheckSupportedController(controller);
            ProgressObserver observer = CreateAndConfigureInstance((IServiceProvider)controller, visualizer, controller.Events, new RelayCommand((s) => controller.TryAbort()), state);

            Debug.Assert(observer != null, "Failed to create observer on the UI thread");

            return observer;
        }

        /// <summary>
        /// Starts observing <see cref="IProgressEvents"/> and visualizes them using <see cref="IProgressVisualizer"/>.
        /// The method is thread safe.
        /// </summary>
        /// <remarks>Will not configure the <see cref="ProgressObserver.CancelCommand"/> to trigger <see cref="IProgressController.TryAbort"/></remarks>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="progressEvents"><see cref="IProgressEvents"/> to observe by</param>
        /// <param name="visualizer"><see cref="IProgressVisualizer"/> to use for visualizing the observed progress</param>
        /// <returns>A new instance of <see cref="ProgressObserver"/> capable observing <see cref="IProgressController"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed on finished")]
        public static ProgressObserver StartObserving(IServiceProvider serviceProvider, IProgressEvents progressEvents, IProgressVisualizer visualizer)
        {
            return StartObserving(serviceProvider, progressEvents, visualizer, null);
        }

        /// <summary>
        /// Starts observing <see cref="IProgressEvents"/> and visualizes them using <see cref="IProgressVisualizer"/>.
        /// The method is thread safe.
        /// </summary>
        /// <remarks>Will not configure the <see cref="ProgressObserver.CancelCommand"/> to trigger <see cref="IProgressController.TryAbort"/></remarks>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="progressEvents"><see cref="IProgressEvents"/> to observe by</param>
        /// <param name="visualizer"><see cref="IProgressVisualizer"/> to use for visualizing the observed progress</param>
        /// <param name="state">Optional. Pre-existing state represented by <see cref="ProgressControllerViewModel"/></param>
        /// <returns>A new instance of <see cref="ProgressObserver"/> capable of observing <see cref="IProgressController"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed on finished")]
        public static ProgressObserver StartObserving(IServiceProvider serviceProvider, IProgressEvents progressEvents, IProgressVisualizer visualizer, ProgressControllerViewModel state)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (progressEvents == null)
            {
                throw new ArgumentNullException(nameof(progressEvents));
            }

            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }

            CheckSupportedProgressEvents(progressEvents);

            ProgressObserver observer = CreateAndConfigureInstance(serviceProvider, visualizer, progressEvents, null, state);

            Debug.Assert(observer != null, "Failed to create observer on the UI thread");

            return observer;
        }

        /// <summary>
        /// Stops the specified <see cref="ProgressObserver"/> from observing.
        /// The method is thread safe.
        /// </summary>
        /// <param name="observer">An existing <see cref="ProgressObserver"/> to stop observing with</param>
        public static void StopObserving(ProgressObserver observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            if (observer.disposed)
            {
                return;
            }

            VsThreadingHelper.RunInline(observer.serviceProvider, VsTaskRunContext.UIThreadNormalPriority, () => ((IDisposable)observer).Dispose());
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// The class will dispose itself once the controller has finished
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // No need to finalize
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Non public static
        /// <summary>
        /// Steps could be hidden which means that they won't be visible. In that case the progress needs to be scaled
        /// to accommodate for those steps and also the visual indicator that a step is running needs be conceptually
        /// correct - which means that all the hidden steps needed to be aggregated to a non-hidden one for visualization
        /// purposes.
        /// </summary>
        /// <param name="steps">The set of <see cref="IProgressStep"/> that need to group</param>
        /// <returns>An array of <see cref="ExecutionGroup"/> that represent the grouping of the actual steps</returns>
        internal static ExecutionGroup[] GroupToExecutionUnits(IEnumerable<IProgressStep> steps)
        {
            Debug.Assert(steps.All(s => s.ImpactsProgress), "Expecting only steps which impact the progress");
            int numberOfExecutionUnits = steps.Count(s => !s.Hidden);
            ExecutionGroup[] groups = new ExecutionGroup[numberOfExecutionUnits];

            for (int i = 0; i < numberOfExecutionUnits; i++)
            {
                groups[i] = new ExecutionGroup();
            }

            // In case no visible steps return an empty array
            if (groups.Length == 0)
            {
                return groups;
            }

            int prevUnit = 0, currentUnit = 0;
            foreach (IProgressStep step in steps)
            {
                // Hidden steps are normally added to the previous visible, unless it
                // is the case in which the hidden steps are first, in which they will be added
                // to the following visible
                if (step.Hidden)
                {
                    groups[prevUnit].Steps.Add(step);
                }
                else
                {
                    Debug.Assert(currentUnit < groups.Length, "Expecting the current unit not to overflow");
                    groups[currentUnit].Steps.Add(step);

                    prevUnit = currentUnit;
                    currentUnit++;
                }
            }

            return groups;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Will dispose on finish")]
        private static ProgressObserver CreateAndConfigureInstance(IServiceProvider serviceProvider, IProgressVisualizer visualizer, IProgressEvents progressEvents, ICommand cancelCommand, ProgressControllerViewModel state)
        {
            return VsThreadingHelper.RunInline(serviceProvider, VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                ProgressObserver returnValue = new ProgressObserver(serviceProvider, visualizer, progressEvents, state);
                returnValue.CancelCommand = cancelCommand;
                return returnValue;
            }, null);
        }

        /// <summary>
        /// Checks if the <see cref="IProgressEvents"/> meets the requirements to be observable by this class
        /// </summary>
        /// <param name="progressEvents">An instance of <see cref="IProgressEvents"/> to check</param>
        private static void CheckSupportedProgressEvents(IProgressEvents progressEvents)
        {
            if (progressEvents.Steps == null)
            {
                throw new InvalidOperationException(ProgressObserverResources.UnsupportedProgressEventsException);
            }
        }

        /// <summary>
        /// Checks if the <see cref="IProgressController"/> meets the requirements to be observable by this class
        /// </summary>
        /// <param name="progressController">An instance of <see cref="IProgressController"/> to check</param>
        private static void CheckSupportedController(IProgressController progressController)
        {
            if (progressController.Events == null || progressController.Events.Steps == null)
            {
                throw new InvalidOperationException(ProgressObserverResources.UnsupportedProgressControllerException);
            }
        }

        /// <summary>
        /// Sets the initial state of a <see cref="ProgressStepViewModel"/>
        /// </summary>
        /// <param name="viewModel">The <see cref="ProgressStepViewModel"/> instance to initialize</param>
        /// <param name="group">The logical group which the <see cref="ProgressStepViewModel"/> represent</param>
        private static void InitializeStep(ProgressStepViewModel viewModel, ExecutionGroup group)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IProgressStep first = group.Steps.First();
            IProgressStep nonHiddenStep = group.Steps.FirstOrDefault(s => !s.Hidden);
            Debug.Assert(nonHiddenStep != null, "The execution group has no visible steps");

            viewModel.DisplayText = nonHiddenStep.DisplayText;
            viewModel.ExecutionState = first.ExecutionState;
            viewModel.Progress.SetUpperBoundLimitedValue(first.Progress);
            viewModel.ProgressDetailText = first.ProgressDetailText;
            viewModel.Progress.IsIndeterminate = group.Steps.Any(s => s.Indeterminate);
        }

        private static double GetCompletedStepCount(IEnumerable<IProgressStep> steps)
        {
            return (double)steps.Count(s => ProgressControllerHelper.IsFinalState(s.ExecutionState));
        }
        #endregion

        #region Event handlers
        private void ControllerStarted(object sender, ProgressEventArgs e)
        {
            // Show can be modal and block, so begin invoke it
            VsThreadingHelper.BeginTask(this.serviceProvider, VsTaskRunContext.UIThreadNormalPriority,
                this.host.Show);
            // Flag that handled to assist with the verification, otherwise the controller will assert
            e.Handled();
        }

        private void ControllerFinished(object sender, ProgressControllerFinishedEventArgs e)
        {
            this.IsFinished = true;

            VsThreadingHelper.RunInline(this.serviceProvider, VsTaskRunContext.BackgroundThread, () =>
            {
                // Give the last progress a chance to render
                System.Threading.Thread.Sleep(DelayAfterFinishInMS);
            });
            VsThreadingHelper.RunInline(this.serviceProvider, VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                this.host.Hide();
                ((IDisposable)this).Dispose();
            });
            // Flag that handled to assist with the verification, otherwise the controller will assert
            e.Handled();
        }

        private void OnCancellationSupportChanged(object sender, CancellationSupportChangedEventArgs e)
        {
            VsThreadingHelper.RunInline(this.serviceProvider, VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                ChangeCancellability(e.Cancellable);
            });
            // Flag that handled to assist with the verification, otherwise the controller will assert
            e.Handled();
        }

        private void OnStepExecutionChanged(object sender, StepExecutionChangedEventArgs e)
        {
            try
            {
                // Don't have to do it on the UI thread since we don't expect the mapping to change during execution
                if (!this.progressStepToViewModelMapping.ContainsKey(e.Step))
                {
                    Debug.Assert(!e.Step.ImpactsProgress, "View model out of sync. The step execution is for unexpected step");
                    return;
                }

                VsThreadingHelper.RunInline(this.serviceProvider, VsTaskRunContext.UIThreadNormalPriority, () =>
                {
                    this.UpdateViewModelStep(e);

                    this.UpdateMainProgress(e);
                });
            }
            finally
            {
                // Flag that handled to assist with the verification, otherwise the controller will assert
                e.Handled();
            }
        }
        #endregion

        #region Private methods
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    this.IsFinished = true;
                    this.progressEvents.Started -= this.ControllerStarted;
                    this.progressEvents.Finished -= this.ControllerFinished;
                    this.progressEvents.StepExecutionChanged -= this.OnStepExecutionChanged;
                    this.progressEvents.CancellationSupportChanged -= this.OnCancellationSupportChanged;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName, ProgressObserverResources.ObserverDisposedException);
            }
        }

        private void InitializeStepViewModels()
        {
            // Although the steps should not change we take a snapshot for safety
            this.steps = this.progressEvents.Steps.ToArray();

            // Create execution groups for all the impacting steps, each group will have one visible step
            IEnumerable<ExecutionGroup> executionGroups = GroupToExecutionUnits(this.ProgressImpactingSteps);
            foreach (ExecutionGroup group in executionGroups)
            {
                ProgressStepViewModel stepViewModel = new ProgressStepViewModel();
                InitializeStep(stepViewModel, group);
                this.viewModelRoot.Steps.Add(stepViewModel);

                // Update mappings
                this.viewModelToExecutionGroupMapping[stepViewModel] = group;
                foreach (IProgressStep step in group.Steps)
                {
                    this.progressStepToViewModelMapping[step] = stepViewModel;
                }
            }
        }

        /// <summary>
        /// Updates the main progress based on the number of steps in final state
        /// an the current step being executed. Each <see cref="IProgressStep"/> which impacts
        /// progress will have one "slot" in the main progress bar.
        /// </summary>
        /// <param name="e">Execution update</param>
        private void UpdateMainProgress(StepExecutionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            double totalNumberOfSteps = this.ProgressImpactingSteps.Count();
            double completedSteps = GetCompletedStepCount(this.ProgressImpactingSteps);

            this.viewModelRoot.Current = this.progressStepToViewModelMapping[e.Step];

            this.viewModelRoot.MainProgress.SetUpperBoundLimitedValue(completedSteps / totalNumberOfSteps);

            // When a determinate step is executing take it's progress into account
            if (!e.IsProgressIndeterminate() && e.State == StepExecutionState.Executing)
            {
                this.viewModelRoot.MainProgress.SetUpperBoundLimitedValue(this.viewModelRoot.MainProgress.Value + e.Progress / totalNumberOfSteps);
            }
        }

        /// <summary>
        /// Updates the <see cref="ProgressStepViewModel"/> with the current step changes.
        /// The <see cref="ProgressStepViewModel"/> represents a <see cref="ExecutionGroup"/>
        /// of one visible step and zero or more hidden steps. The progress in <see cref="ProgressStepViewModel"/>
        /// is used as the sub progress and it will be indeterminate if there's one indeterminate
        /// <see cref="IProgressStep"/> in <see cref="ExecutionGroup"/>, otherwise the sub progress
        /// will be relative to the number of steps in <see cref="ExecutionGroup"/>.
        /// </summary>
        /// <param name="e">Execution update</param>
        private void UpdateViewModelStep(StepExecutionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProgressStepViewModel vm = this.progressStepToViewModelMapping[e.Step];
            ExecutionGroup executionGroup = this.viewModelToExecutionGroupMapping[vm];

            bool isLastStep = e.Step == executionGroup.Steps.Last();
            double groupCompletionPercentange = GetCompletedStepCount(executionGroup.Steps) / (double)executionGroup.Steps.Count;
            this.CurrentExecutingGroup = executionGroup;
            executionGroup.ExecutingStep = e.Step;

            // Update the step VM
            // Progress update: Indeterminate step progress should remain as it was
            // and the determinate step progress should be updated
            switch (e.State)
            {
                case StepExecutionState.NotStarted:
                    Debug.Fail("Unexpected transition to NotStarted");
                    if (!vm.Progress.IsIndeterminate)
                    {
                        vm.Progress.SetUpperBoundLimitedValue(0);
                    }

                    break;
                case StepExecutionState.Executing:
                    if (!vm.Progress.IsIndeterminate)
                    {
                        Debug.Assert(!executionGroup.Steps.Any(s => s.Indeterminate), "Not expecting any Indeterminate steps");

                        vm.Progress.SetUpperBoundLimitedValue(groupCompletionPercentange + (e.Progress / (double)executionGroup.Steps.Count));
                    }

                    vm.ExecutionState = e.State;
                    vm.ProgressDetailText = e.ProgressDetailText;

                    break;
                default:
                    Debug.Assert(ProgressControllerHelper.IsFinalState(e.State), "Unexpected non-final state", "State: {0}", e.State);
                    if (!vm.Progress.IsIndeterminate)
                    {
                        vm.Progress.SetUpperBoundLimitedValue(groupCompletionPercentange);
                    }

                    // Succeeded state which is not the last will indicate
                    // that the group is still executing
                    if (e.State == StepExecutionState.Succeeded && !isLastStep)
                    {
                        vm.ExecutionState = StepExecutionState.Executing;
                    }
                    else
                    {
                        vm.ExecutionState = e.State;
                    }

                    executionGroup.ExecutingStep = null;
                    if (isLastStep)
                    {
                        vm.ProgressDetailText = null;
                        this.CurrentExecutingGroup = null;
                    }

                    break;
            }

            vm.ProgressDetailText = e.ProgressDetailText;
        }

        private void ChangeCancellability(bool cancellable)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.viewModelRoot.Cancellable = cancellable;
        }

        #endregion

        #region Private classes

        /// <summary>
        /// Represents a logical grouping of <see cref="IProgressStep"/> items to a group with a single non-hidden item
        /// that will be used as the visual representative of this group which means that until all the step in this group
        /// haven't finish executing it would seem that the non-hidden step is still executing.
        /// </summary>
        internal /*for testing purposes*/ class ExecutionGroup
        {
            public ExecutionGroup()
            {
                this.Steps = new List<IProgressStep>();
            }

            /// <summary>
            /// Steps in group. Not null.
            /// </summary>
            public IList<IProgressStep> Steps
            {
                get;
                private set;
            }

            /// <summary>
            /// The current <see cref="IProgressStep"/> in a group that is actually executing
            /// </summary>
            public IProgressStep ExecutingStep
            {
                get;
                set;
            }
        }
        #endregion
    }
}
