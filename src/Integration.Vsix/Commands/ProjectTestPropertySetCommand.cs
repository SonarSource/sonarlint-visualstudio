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
    /// Command handler to set the &lt;SonarQubeTestProject/&gt; project property to a specific value.
    /// </summary>
    internal class ProjectTestPropertySetCommand : VsCommandBase
    {
        public const string PropertyName = Constants.SonarQubeTestProjectBuildPropertyKey;
        private readonly IProjectPropertyManager propertyManager;

        private readonly bool? commandPropertyValue;

        internal /*for testing purposes*/ bool? CommandPropertyValue => this.commandPropertyValue;

        /// <summary>
        /// Construct a new command handler to set the &lt;SonarQubeTestProject/&gt;
        /// project property to a specified value.
        /// </summary>
        /// <param name="setPropertyValue">Value this instance will set the project properties to.</param>
        public ProjectTestPropertySetCommand(IServiceProvider serviceProvider, bool? setPropertyValue)
            : base(serviceProvider)
        {
            this.propertyManager = this.ServiceProvider.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");

            this.commandPropertyValue = setPropertyValue;
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.propertyManager != null, "Should not be invokable with no property manager");

            IList<Project> projects = this.propertyManager
                                          .GetSelectedProjects()
                                          .ToList();

            Debug.Assert(projects.Any(), "No projects selected");
            Debug.Assert(projects.All(x => Language.ForProject(x).IsSupported), "Unsupported projects");

            foreach (Project project in projects)
            {
                this.propertyManager.SetBooleanProperty(project, PropertyName, this.commandPropertyValue);
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
                IList<bool?> properties = projects.Select(x =>
                    this.propertyManager.GetBooleanProperty(x, PropertyName)).ToList();

                command.Enabled = true;
                command.Visible = true;
                // Checked if all projects have the same value, and that value is
                // the same as the value this instance is responsible for.
                command.Checked = properties.AllEqual() && (properties.First() == this.commandPropertyValue);
            }
        }
    }
}
