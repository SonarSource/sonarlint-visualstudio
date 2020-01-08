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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Integration.Progress
{
    internal static class ProgressStepRunner
    {
        private readonly static Dictionary<IProgressController, ProgressObserver> observedControllersMap = new Dictionary<IProgressController, ProgressObserver>();

        internal static IReadOnlyDictionary<IProgressController, ProgressObserver> ObservedControllers
        {
            get
            {
                return observedControllersMap;
            }
        }

        internal /*for testing purposes*/ static void Reset()
        {
            observedControllersMap.Clear();
        }

        public static IProgressEvents StartAsync(IServiceProvider sp, IProgressControlHost host, Func<IProgressController, ProgressStepDefinition[]> stepFactory)
        {
            if (sp == null)
            {
                throw new ArgumentNullException(nameof(sp));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (stepFactory == null)
            {
                throw new ArgumentNullException(nameof(stepFactory));
            }

            Debug.Assert(ThreadHelper.CheckAccess(), "Expected to be called on the UI thread");

            // Initialize a controller and an observer
            var controller = new SequentialProgressController(sp);
            controller.Initialize(stepFactory(controller));

            IVsOutputWindowPane sonarLintPane = VsShellUtils.GetOrCreateSonarLintOutputPane(sp);

            bool logFullMessage;
#if DEBUG
            logFullMessage = true;
#else
            logFullMessage = false;
#endif
            var notifier = new VsOutputWindowPaneNotifier(sp,
                sonarLintPane,
                ensureOutputVisible: true,
                messageFormat: Strings.UnexpectedWorkflowError,
                logFullException: logFullMessage);
            controller.ErrorNotificationManager.AddNotifier(notifier);

            Observe(controller, host);
            controller.RunOnFinished(r => observedControllersMap.Remove(controller));
#pragma warning disable 4014 // We do want to start and forget. All the errors will be forwarded via the error notification manager
            controller.StartAsync();
#pragma warning restore 4014

            return controller;
        }

        /// <summary>
        /// Will use the specified <paramref name="host"/> to visualize the progress of <paramref name="controller"/>
        /// </summary>
        public static ProgressObserver Observe(IProgressController controller, IProgressControlHost host)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            Debug.Assert(ThreadHelper.CheckAccess(), "Expected to be called on the UI thread");

            ProgressControl visualizer = VisualizeInHost(host);
            return Observe(controller, visualizer);
        }

        /// <summary>
        /// Re-hosts all the current observers into the specified <paramref name="host"/>
        /// </summary>
        public static void ChangeHost(IProgressControlHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            Debug.Assert(ThreadHelper.CheckAccess(), "Expected to be called on the UI thread");

            Lazy<ProgressControl> visualizer = new Lazy<ProgressControl>(() => VisualizeInHost(host));
            observedControllersMap.ToList().ForEach(kv =>
            {
                if (!kv.Value.IsFinished)
                {
                    ProgressControllerViewModel state = kv.Value.State;
                    kv.Value.Dispose(); // Dispose previous observer
                    observedControllersMap[kv.Key] = CreateObserver(kv.Key, visualizer.Value, state);
                }
            });
        }

        /// <summary>
        /// Aborts all the currently executing controllers
        /// </summary>
        public static void AbortAll()
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected to be called on the UI thread");

            observedControllersMap.ToList().ForEach(kv =>
            {
                kv.Key.TryAbort(); // Try to abort (could already be aborted)
                kv.Value.Dispose(); // Clean up the observer
                observedControllersMap.Remove(kv.Key);
            });

        }

        private static ProgressControl VisualizeInHost(IProgressControlHost host)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected to be called on the UI thread");
            var progressControl = new ProgressControl();
            host.Host(progressControl);
            return progressControl;
        }

        private static ProgressObserver Observe(IProgressController controller, IProgressVisualizer visualizer)
        {
            ProgressObserver observer = CreateObserver(controller, visualizer, null);
            observedControllersMap.Add(controller, observer);
            return observer;
        }

        private static ProgressObserver CreateObserver(IProgressController controller, IProgressVisualizer visualizer, ProgressControllerViewModel state)
        {
            return ProgressObserver.StartObserving(controller, visualizer, state);
        }
    }
}
