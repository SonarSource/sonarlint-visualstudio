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

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Command handler to toggle the &lt;SonarQubeExclude/&gt; project property.
    /// </summary>
    /// <remarks>Will toggle between removing the property, and setting it to 'true'.</remarks>
    internal class ProjectExcludePropertyToggleCommand : VsCommandBase
    {
        public const string PropertyName = Constants.SonarQubeExcludeBuildPropertyKey;

        private readonly IProjectPropertyManager propertyManager;

        public ProjectExcludePropertyToggleCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.propertyManager = this.ServiceProvider.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.propertyManager != null, "Should not be invokable with no property manager");

            IList<Project> projects = this.propertyManager
                                          .GetSelectedProjects()
                                          .ToList();

            Debug.Assert(projects.Any(), "No projects selected");
            Debug.Assert(projects.All(x => Language.ForProject(x).IsSupported), "Unsupported projects");

            if (projects.Count == 1 ||
                projects.Select(x => this.propertyManager.GetBooleanProperty(x, PropertyName)).AllEqual())
            {
                // Single project, or multiple projects & consistent property values
                foreach (Project project in projects)
                {
                    this.ToggleProperty(project);
                }
            }
            else
            {
                // Multiple projects & mixed property values
                foreach (Project project in projects)
                {
                    this.propertyManager.SetBooleanProperty(project, PropertyName, true);
                }
            }
        }

        private void ToggleProperty(Project project)
        {
            bool currentValue = this.propertyManager.GetBooleanProperty(project, PropertyName)
                                                            .GetValueOrDefault(false);
            if (currentValue)
            {
                this.propertyManager.SetBooleanProperty(project, PropertyName, null);
            }
            else
            {
                this.propertyManager.SetBooleanProperty(project, PropertyName, true);
            }
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
                IList<bool> properties = projects.Select(x =>
                    this.propertyManager.GetBooleanProperty(x, PropertyName) ?? false).ToList();

                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && properties.First();
            }
        }
    }
}
