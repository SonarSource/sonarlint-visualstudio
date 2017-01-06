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

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Vsix;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsCommand : VsCommandBase
    {
        private readonly Action<OleMenuCommand> queryStatusFunc;

        public int InvokationCount { get; private set; } = 0;

        public ConfigurableVsCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public ConfigurableVsCommand(IServiceProvider serviceProvider, Action<OleMenuCommand> queryStatusFunc)
            : base(serviceProvider)
        {
            this.queryStatusFunc = queryStatusFunc;
        }

        #region VsCommandBase

        protected override void InvokeInternal()
        {
            this.InvokationCount++;
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            this.queryStatusFunc?.Invoke(command);
        }

        #endregion
    }
}
