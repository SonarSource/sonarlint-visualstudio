//-----------------------------------------------------------------------
// <copyright file="RelayCommand.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Progress.MVVM
{
    /// MSDN Implementation - http://msdn.microsoft.com/en-us/magazine/dd419663.aspx#id0090030
    /// <summary>
    /// A command whose sole purpose is to
    /// relay its functionality to other
    /// objects by invoking delegates. The
    /// default return value for the CanExecute
    /// method is 'true'.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Predicate<object> canExecute;
        private readonly object lockObject = new object();
        private EventHandler canExecuteChanged;

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action execute)
            : this((obj) => execute(), null)
        {
        }

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Raised when CanExecute changes.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                lock (this.lockObject)
                {
                    this.canExecuteChanged += value;
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                lock (this.lockObject)
                {
                    this.canExecuteChanged -= value;
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        [DebuggerStepThrough]
        public virtual bool CanExecute(object parameter)
        {
            if (null == this.canExecute)
            {
                return true;
            }

            return this.canExecute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            this.canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            Debug.Assert(this.CanExecute(parameter));
            this.execute(parameter);
        }
    }
}
