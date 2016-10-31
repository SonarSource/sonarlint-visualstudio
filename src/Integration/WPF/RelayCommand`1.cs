//-----------------------------------------------------------------------
// <copyright file="RelayCommand`1.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
