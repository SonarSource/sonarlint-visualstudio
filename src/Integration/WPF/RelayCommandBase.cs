//-----------------------------------------------------------------------
// <copyright file="RelayCommandBase.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.WPF
{
    internal abstract class RelayCommandBase : ICommand
    {
        private event EventHandler canExecuteChanged;

        public void RequeryCanExecute()
        {
            if (System.Windows.Application.Current != null)
            {
                Debug.Assert(System.Windows.Application.Current.Dispatcher.CheckAccess(), "RequeryCanExecute should be called from the UI thread");
            }

            this.canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract bool CanExecute(object parameter);

        protected abstract void Execute(object parameter);

        #region ICommand

        public event EventHandler CanExecuteChanged
        {
            add
            {
                this.canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                this.canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return this.CanExecute(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            this.Execute(parameter);
        }

        #endregion
    }
}
