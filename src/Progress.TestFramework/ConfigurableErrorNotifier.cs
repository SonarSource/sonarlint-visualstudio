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
using System.Collections.Generic;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressErrorNotifier"/>
    /// </summary>
    internal class ConfigurableErrorNotifier : IProgressErrorNotifier
    {
        public ConfigurableErrorNotifier()
        {
            this.Reset();
        }

        #region Customization properties

        public Action<Exception> NotifyAction
        {
            get;
            set;
        }

        public List<Exception> Exceptions
        {
            get;
            private set;
        }

        #endregion Customization properties

        #region Customization and verification methods

        public void Reset()
        {
            this.Exceptions = new List<Exception>();
            this.NotifyAction = null;
        }

        #endregion Customization and verification methods

        #region Test implementation of IProgressErrorHandler (not to be used explicitly by the test code)

        void IProgressErrorNotifier.Notify(Exception ex)
        {
            this.Exceptions.Add(ex);
            this.NotifyAction?.Invoke(ex);
        }

        #endregion Test implementation of IProgressErrorHandler (not to be used explicitly by the test code)
    }
}