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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Progress.Controller.ErrorNotification
{
    /// <summary>
    /// <see cref="IProgressErrorNotifier"/> that notifies using the Message box
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Notifier", Justification = "False positive")]
    public sealed class VsMessageBoxNotifier : IProgressErrorNotifier
    {
        private readonly IServiceProvider serviceProvider;
        private readonly string messageTitle;
        private readonly string messageFormat;
        private readonly bool logWholeMessage;

        /// <summary>
        /// Constructor for <see cref="VsMessageBoxNotifier"/>
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> instance. Required.</param>
        /// <param name="title">Required message box title</param>
        /// <param name="messageFormat">Required. Expected to have only one placeholder</param>
        /// <param name="logWholeMessage">Whether to shown the exception message or the whole exception</param>
        public VsMessageBoxNotifier(IServiceProvider serviceProvider, string title, string messageFormat, bool logWholeMessage)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (string.IsNullOrWhiteSpace(messageFormat))
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            this.serviceProvider = serviceProvider;
            this.messageTitle = title;
            this.messageFormat = messageFormat;
            this.logWholeMessage = logWholeMessage;
        }

        void IProgressErrorNotifier.Notify(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            VsShellUtilities.ShowMessageBox(this.serviceProvider,
               ProgressControllerHelper.FormatErrorMessage(ex, this.messageFormat, this.logWholeMessage),
               this.messageTitle,
               OLEMSGICON.OLEMSGICON_CRITICAL,
               OLEMSGBUTTON.OLEMSGBUTTON_OK,
               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
