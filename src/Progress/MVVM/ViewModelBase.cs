/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Progress.MVVM
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        private readonly Dispatcher dispatcher;

#if DEBUG
        private readonly string stackPrint;
#endif
        protected ViewModelBase()
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;

#if DEBUG
            this.stackPrint = new StackTrace().ToString();
#endif
        }

        /// <summary>
        ///  Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Dispatcher associated with this object.
        /// </summary>
        public Dispatcher Dispatcher
        {
            get { return this.dispatcher; }
        }

        /// <summary>
        /// Determines whether the calling thread has access to this object.
        /// </summary>
        /// <returns>true if the calling thread has access to this object; otherwise, false.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool CheckAccess()
        {
            return Dispatcher.CheckAccess();
        }

        /// <summary>
        /// Executes the specified action on a thread associated with object's dispatcher.
        /// </summary>
        /// <param name="action">An action to execute.</param>
        public void CheckAccessInvoke(Action action)
        {
            ArgumentValidation.NotNull(action, "action");

            if (this.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.Invoke(action, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Executes the specified action on a thread associated with object's dispatcher.
        /// This invokes a InvokeAsync on the Dispatcher, does not wait for the action
        /// to complete -- returns immediately.
        /// </summary>
        /// <param name="action">An action to execute.</param>
        /// <returns>A task that completes when action has completed.</returns>
        public async Task CheckAccessInvokeAsync(Action action)
        {
            ArgumentValidation.NotNull(action, "action");

            if (this.CheckAccess())
            {
                action();
            }
            else
            {
                await Dispatcher.InvokeAsync(action, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Executes the specified action on a thread associated with object's dispatcher.
        /// </summary>
        /// <param name="func">An action to execute.</param>
        /// <returns>The result of the action.</returns>
        public TResult CheckAccessInvoke<TResult>(Func<TResult> func)
        {
            ArgumentValidation.NotNull(func, "action");

            if (this.CheckAccess())
            {
                return func();
            }
            else
            {
                return Dispatcher.Invoke(func, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Executes the specified function on a thread associated with object's dispatcher.
        /// This invokes a InvokeAsync on the Dispatcher, does not wait for the action
        /// to complete -- returns immediately.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        /// <returns>A task with the result of func when completed.</returns>
        public async Task<TResult> CheckAccessInvokeAsync<TResult>(Func<TResult> func)
        {
            ArgumentValidation.NotNull(func, "action");

            if (this.CheckAccess())
            {
                return func();
            }
            else
            {
                return await Dispatcher.InvokeAsync(func, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Enforces that the calling thread has access to this object.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The calling thread does not have access to this object.</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected void VerifyAccess()
        {
#if DEBUG
            try
            {
                Dispatcher.VerifyAccess();
            }
            catch (Exception ex)
            {
                // Assist with debugging by providing the stack when created (access stack will be available during runtime)
                throw new Exception(this.stackPrint, ex);
            }
#else
            Dispatcher.VerifyAccess();
#endif
        }

        /// <summary>
        /// Raises PropertyChanged event. This method can only be called on the thread associated with this object's dispatcher.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <exception cref="System.InvalidOperationException">The calling thread does not have access to this object.</exception>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "No")]
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Set by [CallerMemberName]")]
        protected virtual void RaisePropertyChanged([CallerMemberName]string propertyName = null)
        {
            this.VerifyAccess();
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
