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
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public abstract class VsCommandBase
    {
        private readonly IServiceProvider serviceProvider;

        protected IServiceProvider ServiceProvider => this.serviceProvider;

        protected VsCommandBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        protected virtual void QueryStatusInternal(OleMenuCommand command)
        {
        }

        protected abstract void InvokeInternal();

        public void Invoke(object sender, EventArgs args)
        {
            var command = sender as OleMenuCommand;
            Debug.Assert(command != null, "Unexpected sender type; expected OleMenuCommand");
            Debug.Assert(command.Enabled, "Tried to invoke command without it being enabled");
            if (command.Enabled)
            {
                this.InvokeInternal();
            }
        }

        public void QueryStatus(object sender, EventArgs args)
        {
            var command = sender as OleMenuCommand;
            Debug.Assert(command != null, "Unexpected sender type; expected OleMenuCommand");
            this.QueryStatusInternal(command);
        }
    }
}
