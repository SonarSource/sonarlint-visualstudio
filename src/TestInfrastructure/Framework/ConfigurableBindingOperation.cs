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
using System.Threading;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableBindingOperation : IBindingOperation
    {
        #region IBindingOperation
        void IBindingOperation.Initialize()
        {
            this.InitializeAction?.Invoke();
        }

        void IBindingOperation.Commit()
        {
            this.CommitAction?.Invoke();
        }

        void IBindingOperation.Prepare(CancellationToken token)
        {
            this.PrepareAction?.Invoke(token);
        }
        #endregion


        #region Test helpers
        public Action InitializeAction { get; set; }

        public Action<CancellationToken> PrepareAction { get; set; }

        public Action CommitAction { get; set; }
        #endregion
    }
}
