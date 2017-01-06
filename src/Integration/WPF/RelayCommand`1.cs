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
