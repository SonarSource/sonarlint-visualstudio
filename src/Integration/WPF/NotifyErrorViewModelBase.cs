//-----------------------------------------------------------------------
// <copyright file="NotifyErrorViewModelBase.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
