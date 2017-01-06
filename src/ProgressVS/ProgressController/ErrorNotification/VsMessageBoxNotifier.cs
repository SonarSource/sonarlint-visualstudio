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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

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
