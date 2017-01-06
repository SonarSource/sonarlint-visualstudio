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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using System;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IProjectPropertyManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectPropertyManager : IProjectPropertyManager
    {
        private readonly IProjectSystemHelper projectSystem;

        [ImportingConstructor]
        public ProjectPropertyManager(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.projectSystem = host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
        }

        #region IProjectPropertyManager

        public IEnumerable<Project> GetSelectedProjects()
        {
            return this.projectSystem
                .GetSelectedProjects();
        }

        public bool? GetBooleanProperty(Project project, string propertyName)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            string propertyString = this.projectSystem.GetProjectProperty(project, propertyName);

            bool propertyValue;
            if (bool.TryParse(propertyString, out propertyValue))
            {
                return propertyValue;
            }

            return null;
        }

        public void SetBooleanProperty(Project project, string propertyName, bool? value)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (value.HasValue)
            {
                this.projectSystem.SetProjectProperty(project, propertyName, value.Value.ToString());
            }
            else
            {
                this.projectSystem.ClearProjectProperty(project, propertyName);
            }
        }

        #endregion
    }
}
