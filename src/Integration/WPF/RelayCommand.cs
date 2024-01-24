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
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.WPF
{
    /// <summary>
    /// A command whose sole purpose is to relay its functionality to other objects by invoking delegates.
    /// The default return value for the CanExecute method is 'True'.
    /// </summary>
    public class RelayCommand : RelayCommandBase
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public RelayCommand(Action execute) :
            this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute()
        {
            // can be called directly from XAML on the UI thread so we need to guard against unhandled exceptions
            try
            {
                return this.canExecute?.Invoke() ?? true;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Just squash the exception
                return false;
            }
        }

        public void Execute()
        {
            // can be called directly from XAML on the UI thread so we need to guard against unhandled exceptions
            try
            {
                this.execute();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Just squash the exception
            }
        }

        #region RelayCommandBase

        protected override bool CanExecute(object parameter)
        {
            return this.CanExecute();
        }

        protected override void Execute(object parameter)
        {
            this.Execute();
        }

        #endregion
    }
}
