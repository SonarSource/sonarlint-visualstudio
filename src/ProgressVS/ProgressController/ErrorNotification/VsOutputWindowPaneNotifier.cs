/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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

using SonarLint.VisualStudio.Progress.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Progress.Controller.ErrorNotification
{
    /// <summary>
    /// <see cref="IProgressErrorNotifier"/> that notifies using any output window pane
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Notifier", Justification = "False positive")]
    public sealed class VsOutputWindowPaneNotifier : IProgressErrorNotifier
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IVsOutputWindowPane pane;
        private readonly bool ensureOutputVisible;
        private readonly string messageFormat;
        private readonly bool logFullException;

        /// <summary>
        /// Constructor for <see cref="VsOutputWindowPaneNotifier"/>
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> instance. Required.</param>
        /// <param name="pane">The <seealso cref="IVsOutputWindowPane"/> to use</param>
        /// <param name="ensureOutputVisible">Whether to shown and activate the output window</param>
        /// <param name="messageFormat">Required. Expected to have only one placeholder</param>
        /// <param name="logFullException">Whether to shown the exception message or the whole exception</param>
        public VsOutputWindowPaneNotifier(IServiceProvider serviceProvider, IVsOutputWindowPane pane, bool ensureOutputVisible, string messageFormat, bool logFullException)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (pane == null)
            {
                throw new ArgumentNullException(nameof(pane));
            }

            if (string.IsNullOrWhiteSpace(messageFormat))
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            this.serviceProvider = serviceProvider;
            this.pane = pane;
            this.ensureOutputVisible = ensureOutputVisible;
            this.messageFormat = messageFormat;
            this.logFullException = logFullException;
        }

        void IProgressErrorNotifier.Notify(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            VsThreadingHelper.RunInline(this.serviceProvider, VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                int hr = this.pane.OutputStringThreadSafe(ProgressControllerHelper.FormatErrorMessage(ex, this.messageFormat, this.logFullException) + Environment.NewLine);

                if (this.ensureOutputVisible && ErrorHandler.Succeeded(hr) && ErrorHandler.Succeeded(pane.Activate()))
                {
                    ShowOutputWindowFrame(serviceProvider);
                }
            });
        }

        #region Private methods
        /// <summary>
        /// Shows the output window frame
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame.Show", Justification = "Failure to show the output pane is merely unfortunate")]
        private static void ShowOutputWindowFrame(IServiceProvider serviceProvider)
        {
            Debug.Assert(serviceProvider != null, "Supplied service provider should not be null");

            // Calling "Activate" on its own isn't enough if the Output window isn't already being shown
            // we need to explicitly call "Show" on the window.
            IVsUIShell shell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell != null)
            {
                IVsWindowFrame windowFrame;
                int hr = shell.FindToolWindow(0, VSConstants.StandardToolWindows.Output, out windowFrame);
                Debug.Assert(ErrorHandler.Succeeded(hr), "Call to IVsUIShell FindTooWindow failed.");
                if (ErrorHandler.Succeeded(hr) && windowFrame != null)
                {
                    windowFrame.Show();
                }
            }
        }
        #endregion
    }
}
