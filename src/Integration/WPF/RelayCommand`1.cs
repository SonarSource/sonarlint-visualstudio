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
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.WPF
{
    /// <summary>
    /// A command whose sole purpose is to relay its functionality to other objects by invoking delegates.
    /// The default return value for the CanExecute method is 'True'.
    /// Will pass default(<typeparamref name="T"/>) in as the parameters if type cast fails.
    /// </summary>
    /// <remarks>
    /// Restricted <typeparamref name="T"/> to be a reference type only. Removing this restriction and using
    /// default(<typeparamref name="T"/>) for value types was considered but rejected because it would not
    /// be possible to differentiate between an invalid value type cast and an actual default value of <typeparamref name="T"/>.
    /// To use value types with this class you should box your type in a <see cref="Nullable{T}"/>.
    /// </remarks>
    /// <typeparam name="T"><see cref="Execute(T)"/> and <see cref="CanExecute(T)"/> parameter type</typeparam>
    internal class RelayCommand<T> : RelayCommandBase where T : class
    {
        private readonly Action<T> execute;
        private readonly Predicate<T> canExecute;

        public RelayCommand(Action<T> execute) :
            this(execute, null)
        {
        }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(T parameter)
        {
            return this.canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(T parameter)
        {
            this.execute(parameter);
        }

        internal /* testing purposes */ static T SafeCast(object parameter)
        {
            // Can't assert - dispatcher is suspended, asserting will crash the application.
            return parameter as T;
        }

        #region RelayCommandBase

        protected override bool CanExecute(object parameter)
        {
            return this.CanExecute(SafeCast(parameter));
        }

        protected override void Execute(object parameter)
        {
            this.Execute(SafeCast(parameter));
        }

        #endregion
    }
}
