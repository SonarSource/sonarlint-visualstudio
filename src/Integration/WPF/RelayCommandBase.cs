/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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
