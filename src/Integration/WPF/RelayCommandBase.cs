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
