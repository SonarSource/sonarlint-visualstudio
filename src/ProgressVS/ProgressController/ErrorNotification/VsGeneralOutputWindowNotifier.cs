//-----------------------------------------------------------------------
// <copyright file="VsGeneralOutputWindowNotifier.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Progress.Controller.ErrorNotification
{
    /// <summary>
    /// <see cref="IProgressErrorNotifier"/> that notifies using the general ouput window
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Notifier", Justification = "False positive")]
    public sealed class VsGeneralOutputWindowNotifier : IProgressErrorNotifier
    {
        private readonly IServiceProvider serviceProvider;
        private readonly bool ensureOutputVisible;
        private readonly string messageFormat;
        private readonly bool logWholeMessage;

        /// <summary>
        /// Constructor for <see cref="VsGeneralOutputWindowNotifier"/>
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> instance. Required.</param>
        /// <param name="ensureOutputVisible">Whether to shown and activate the output window</param>
        /// <param name="messageFormat">Required. Expected to have only one placeholder</param>
        /// <param name="logWholeMessage">Whether to shown the exception message or the whole exception</param>
        public VsGeneralOutputWindowNotifier(IServiceProvider serviceProvider, bool ensureOutputVisible, string messageFormat, bool logWholeMessage)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (string.IsNullOrWhiteSpace(messageFormat))
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            this.serviceProvider = serviceProvider;
            this.ensureOutputVisible = ensureOutputVisible;
            this.messageFormat = messageFormat;
            this.logWholeMessage = logWholeMessage;
        }

        void IProgressErrorNotifier.Notify(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            VsThreadingHelper.RunInline(this.serviceProvider, VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                IVsOutputWindowPane generalPanel = serviceProvider.GetService(typeof(SVsGeneralOutputWindowPane)) as IVsOutputWindowPane;
                Debug.Assert(generalPanel != null, "Cannot find the general output window");
                if (generalPanel != null)
                {
                    if (ErrorHandler.Succeeded(generalPanel.OutputStringThreadSafe(ProgressControllerHelper.FormatErrorMessage(ex, this.messageFormat, this.logWholeMessage) + Environment.NewLine)))
                    {
                        if (this.ensureOutputVisible)
                        {
                            if (ErrorHandler.Succeeded(generalPanel.Activate()))
                            {
                                ShowGeneralOutputPane(serviceProvider);
                            }
                        }
                    }
                }
            });
        }

        #region Private methods
        /// <summary>
        /// Shows the general output pane
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame.Show", Justification = "Failure to show the output pane is merely unfortunate")]
        private static void ShowGeneralOutputPane(IServiceProvider serviceProvider)
        {
            Debug.Assert(serviceProvider != null, "Supplied service provider should not be null");

            // Calling "Activate" on its own isn't enough if the Output window isn't already being shown;
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
