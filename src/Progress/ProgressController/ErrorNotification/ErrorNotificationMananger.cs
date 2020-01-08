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
