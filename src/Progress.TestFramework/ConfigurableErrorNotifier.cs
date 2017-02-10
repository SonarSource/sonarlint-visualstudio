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
using System.Collections.Generic;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressErrorNotifier"/>
    /// </summary>
    public class ConfigurableErrorNotifier : IProgressErrorNotifier
    {
        public ConfigurableErrorNotifier()
        {
            this.Reset();
        }

        #region Customization properties

        public Action<Exception> NotifyAction
        {
            get;
            set;
        }

        public List<Exception> Exceptions
        {
            get;
            private set;
        }

        #endregion Customization properties

        #region Customization and verification methods

        public void Reset()
        {
            this.Exceptions = new List<Exception>();
            this.NotifyAction = null;
        }

        #endregion Customization and verification methods

        #region Test implementation of IProgressErrorHandler (not to be used explicitly by the test code)

        void IProgressErrorNotifier.Notify(Exception ex)
        {
            this.Exceptions.Add(ex);
            this.NotifyAction?.Invoke(ex);
        }

        #endregion Test implementation of IProgressErrorHandler (not to be used explicitly by the test code)
    }
}