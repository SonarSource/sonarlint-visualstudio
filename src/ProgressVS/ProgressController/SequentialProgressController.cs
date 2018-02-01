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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// An implementation of <see cref="IProgressController"/> which will process the steps in order that they were passed in.
    /// In case of a failure or cancellation the controller will not proceed to the following steps.
    /// <remarks>The controller can be started only once.</remarks>
    /// </summary>
    /// <example>
    /// Usage example
    /// <code>
    /// SequentialProgressController controller = new SequentialProgressController(...);
    /// controller.Initialize(new ProgressStepDefinition[] { ... });
    ///
    /// // Create an observer
    /// ProgressObserver observer = ProgressObserver.StartObserving(controller);
    /// observer.DisplayTitle = "title";
    ///
    /// // Start the controller and await for finish
    /// ProgressControllerResult result = await controller.Start();
    /// </code>
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "cancellationTokenSource is being disposed OnFinish wish is guaranteed (tested) to be called in the end")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
    "S2931:Classes with \"IDisposable\" members should implement \"IDisposable\"",
        Justification = "cancellationTokenSource is being disposed OnFinish wish is guaranteed (tested) to be called in the end",
        Scope = "type",
        Target = "~T:SonarLint.VisualStudio.Progress.Controller.SequentialProgressController")]
    internal sealed partial class SequentialProgressController : IProgressEvents
    {
        #region Fields
        private readonly IServiceProvider serviceProvider;
        private bool canAbort;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs a sequential progress controller
        /// </summary>
        /// <param name="serviceProvider">Required service provider</param>
        public SequentialProgressController(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Whether the current executing step can be aborted
        /// </summary>
        public bool CanAbort
        {
            get
            {
                return this.canAbort;
            }

            private set
            {
                if (this.canAbort != value)
                {
                    this.canAbort = value;
                    this.OnCancellableChanged(this.canAbort);
                }
            }
        }

        /// <summary>
        /// Whether the controller is started
        /// </summary>
        public bool IsStarted
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether the controller is finished
        /// </summary>
        public bool IsFinished
        {
            get;
            private set;
        }
        #endregion

        #region Static
        /// <summary>
        /// Creates initializes <see cref="SequentialProgressController"/>
        /// </summary>
        /// <param name="serviceProvider">Service provider instance. Required.</param>
        /// <param name="stepsDefinition">One or more instance of <see cref="ProgressStepDefinition"/></param>
        /// <returns>The initialized <see cref="SequentialProgressController"/></returns>
        public static SequentialProgressController Create(IServiceProvider serviceProvider, params ProgressStepDefinition[] stepsDefinition)
        {
            SequentialProgressController controller = new SequentialProgressController(serviceProvider);
            controller.Initialize(stepsDefinition);
            return controller;
        }

        /// <summary>
        /// Creates initializes <see cref="SequentialProgressController"/>
        /// </summary>
        /// <param name="serviceProvider">Service provider instance. Required.</param>
        /// <param name="stepFactory"><see cref="IProgressStepFactory"/> to use when create steps from definitions</param>
        /// <param name="stepsDefinition">One or more instance of <see cref="IProgressStepDefinition"/></param>
        /// <returns>The initialized <see cref="SequentialProgressController"/></returns>
        public static SequentialProgressController Create(IServiceProvider serviceProvider, IProgressStepFactory stepFactory, params IProgressStepDefinition[] stepsDefinition)
        {
            SequentialProgressController controller = new SequentialProgressController(serviceProvider);
            controller.Initialize(stepFactory, stepsDefinition);
            return controller;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Initializes the controller with a set of <see cref="ProgressStepDefinition"/>
        /// </summary>
        /// <param name="stepsDefinition">The array of <see cref="ProgressStepDefinition"/> to initialize the controller with</param>
        public void Initialize(params ProgressStepDefinition[] stepsDefinition)
        {
            this.Initialize(new DefaultProgressStepFactory(), stepsDefinition);
        }
        #endregion

        #region IServiceProvider
        /// <summary>
        /// Returns the instance of a service for the specified service type
        /// </summary>
        /// <param name="serviceType">The type of the requested service</param>
        /// <returns>Can be null in case no such service type is available</returns>
        public object GetService(Type serviceType)
        {
            if (this.serviceProvider == null)
            {
                return null;
            }

            return this.serviceProvider.GetService(serviceType);
        }
        #endregion
    }
}
