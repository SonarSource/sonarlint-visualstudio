//-----------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SonarLint.VisualStudio.Integration.WPF
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void RaisePropertyChanged([CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SetAndRaisePropertyChanged<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            bool equal = false;

            if (value is IEquatable<T>)
            {
                equal = ((IEquatable<T>)value).Equals(field);
            }
            else if (typeof(T).IsSubclassOf(typeof(Enum)))
            {
                equal = Enum.Equals(value, field);
            }
            else
            {
                equal = ReferenceEquals(value, field);
            }

            if (!equal)
            {
                field = value;
                RaisePropertyChanged(propertyName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SetAndRaisePropertyChanged(ref string field, string value, [CallerMemberName]string propertyName = null)
        {
            if (!string.Equals(field, value, StringComparison.Ordinal))
            {
                field = value;
                RaisePropertyChanged(propertyName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected bool SetAndRaisePropertyChanged(ref bool propertyDataField, bool value, [CallerMemberName]string propertyName = null)
        {
            if (propertyDataField != value)
            {
                propertyDataField = value;
                RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }
    }
}
