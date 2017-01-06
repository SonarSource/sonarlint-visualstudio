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

namespace SonarLint.VisualStudio.Progress.Controller.ErrorNotification
{
    /// <summary>
    /// Implementation of <see cref="IErrorNotificationManager"/>
    /// that allows to addition and removal of <see cref="IProgressErrorNotifier"/> that will be notified of errors
    /// </summary>
    /// <remarks>The class is thread safe and each <see cref="IProgressErrorNotifier"/> is responsible for notifying on the correct thread</remarks>
    public sealed class ErrorNotificationManager : IErrorNotificationManager
    {
        #region Private fields and consts
        private readonly HashSet<IProgressErrorNotifier> notifiers = new HashSet<IProgressErrorNotifier>();
        private readonly object notifiersLock = new object();
        #endregion

        #region IErrorNotificationManager
        /// <summary>
        /// <see cref="IErrorNotificationManager.AddNotifier"/>
        /// </summary>
        /// <param name="notifier">A unique instance of <see cref="IProgressErrorNotifier"/></param>
        public void AddNotifier(IProgressErrorNotifier notifier)
        {
            if (notifier == null)
            {
                throw new ArgumentNullException(nameof(notifier));
            }

            lock (this.notifiersLock)
            {
                this.notifiers.Add(notifier);
            }
        }

        /// <summary>
        /// <see cref="IErrorNotificationManager.RemoveNotifier"/>
        /// </summary>
        /// <param name="notifier">An existing instance of <see cref="IProgressErrorNotifier"/></param>
        public void RemoveNotifier(IProgressErrorNotifier notifier)
        {
            if (notifier == null)
            {
                throw new ArgumentNullException(nameof(notifier));
            }

            lock (this.notifiersLock)
            {
                this.notifiers.Remove(notifier);
            }
        }

        #endregion

        #region IProgressErrorNotifier
        void IProgressErrorNotifier.Notify(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            lock (this.notifiersLock)
            {
                foreach (IProgressErrorNotifier notifier in this.notifiers)
                {
                    notifier.Notify(ex);
                }
            }
        }
        #endregion
    }
}
