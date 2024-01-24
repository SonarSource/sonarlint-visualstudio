/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SonarLint.VisualStudio.Progress.MVVM
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        ///  Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises PropertyChanged event. This method can only be called on the thread associated with this object's dispatcher.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <exception cref="System.InvalidOperationException">The calling thread does not have access to this object.</exception>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "No")]
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Set by [CallerMemberName]")]
        protected virtual void RaisePropertyChanged([CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// A helper method that sets property value and raises PropertyChanged event if the value has changed.
        /// </summary>
        /// <param name="propertyDataField">A reference to the data member which is used to store property value.</param>
        /// <param name="value">New property value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property value has changed, false otherwise.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#", Justification = "This method is designed to atomically set a value and raise an event, ref makes sense in this case")]
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Set by [CallerMemberName]")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected bool SetAndRaisePropertyChanged<T>(ref T propertyDataField, T value, [CallerMemberName] string propertyName = null)
        {
            bool equal;
            IEquatable<T> equatableValue = value as IEquatable<T>;

            if (equatableValue != null)
            {
                equal = equatableValue.Equals(propertyDataField);
            }
            else if (typeof(T).IsSubclassOf(typeof(Enum)))
            {
                equal = Equals(value, propertyDataField);
            }
            else
            {
                equal = Equals(value, propertyDataField);
            }

            if (!equal)
            {
                propertyDataField = value;
                this.RaisePropertyChanged(propertyName);
            }

            return !equal;
        }

        /// <summary>
        /// A helper method that sets property value and raises PropertyChanged event if the value has changed.
        /// Optimized implementation for string type.
        /// </summary>
        /// <param name="propertyDataField">A reference to the data member which is used to store property value.</param>
        /// <param name="value">New property value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property value has changed, false otherwise.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#", Justification = "This method is designed to atomically set a value and raise an event, ref makes sense in this case")]
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Set by [CallerMemberName]")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected bool SetAndRaisePropertyChanged(ref string propertyDataField, string value, [CallerMemberName] string propertyName = null)
        {
            if (!string.Equals(propertyDataField, value, StringComparison.Ordinal))
            {
                propertyDataField = value;
                this.RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// A helper method that sets property value and raises PropertyChanged event if the value has changed.
        /// Optimized implementation for System.Int32 type.
        /// </summary>
        /// <param name="propertyDataField">A reference to the data member which is used to store property value.</param>
        /// <param name="value">New property value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property value has changed, false otherwise.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#", Justification = "This method is designed to atomically set a value and raise an event, ref makes sense in this case")]
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Set by [CallerMemberName]")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected bool SetAndRaisePropertyChanged(ref int propertyDataField, int value, [CallerMemberName] string propertyName = null)
        {
            if (propertyDataField != value)
            {
                propertyDataField = value;
                this.RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// A helper method that sets property value and raises PropertyChanged event if the value has changed.
        /// Optimized implementation for System.Boolean type.
        /// </summary>
        /// <param name="propertyDataField">A reference to the data member which is used to store property value.</param>
        /// <param name="value">New property value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property value has changed, false otherwise.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#", Justification = "This method is designed to atomically set a value and raise an event, ref makes sense in this case")]
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Set by [CallerMemberName]")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected bool SetAndRaisePropertyChanged(ref bool propertyDataField, bool value, [CallerMemberName] string propertyName = null)
        {
            if (propertyDataField != value)
            {
                propertyDataField = value;
                this.RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }

    }
}
