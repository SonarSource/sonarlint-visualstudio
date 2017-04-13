/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SonarLint.VisualStudio.Integration.WPF
{
    internal abstract class NotifyErrorViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public abstract bool HasErrors { get; }

        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            string error = null;
            if (this.GetErrorForProperty(propertyName, ref error))
            {
                return new[] { error };
            }
            return null;
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void RaiseErrorsChanged([CallerMemberName] string propertyName = null)
        {
            this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected abstract bool GetErrorForProperty(string propertyName, ref string error);
    }
}
