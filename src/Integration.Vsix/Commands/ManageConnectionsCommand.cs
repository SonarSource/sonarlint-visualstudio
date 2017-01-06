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
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class ManageConnectionsCommand : VsCommandBase
    {
        private readonly ITeamExplorerController teamExplorer;

        public ManageConnectionsCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.teamExplorer = this.ServiceProvider.GetMefService<ITeamExplorerController>();
            Debug.Assert(this.teamExplorer != null, "Couldn't get Team Explorer controller from MEF");
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = (this.teamExplorer != null);
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.teamExplorer != null, "Should only be invocable with a handle to the team explorer controller");
            this.teamExplorer.ShowSonarQubePage();
        }
    }
}
