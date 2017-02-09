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
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Command handler for the SonarLint menu.
    /// </summary>
    /// <remarks>Has no invocation logic; only has QueryStatus</remarks>
    internal class ProjectSonarLintMenuCommand : VsCommandBase
    {
        private readonly IProjectPropertyManager propertyManager;

        public ProjectSonarLintMenuCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.propertyManager = this.ServiceProvider.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");
        }

        protected override void InvokeInternal()
        {
            // Do nothing; this is a menu only.
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;
            if (this.propertyManager == null)
            {
                return;
            }

            IList<Project> projects = this.propertyManager
                                          .GetSelectedProjects()
                                          .ToList();

            if (projects.Any() && projects.All(x => Language.ForProject(x).IsSupported))
            {
                command.Enabled = true;
                command.Visible = true;
            }
        }
    }
}
