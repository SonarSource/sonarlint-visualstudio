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
            bool equal;

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
