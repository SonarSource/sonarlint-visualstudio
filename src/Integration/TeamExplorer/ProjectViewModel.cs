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

using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal class ProjectViewModel : ViewModelBase
    {
        private readonly ContextualCommandsCollection commands = new ContextualCommandsCollection();
        private bool isBound;

        // Ordinal comparer should be good enough: http://docs.sonarqube.org/display/SONAR/Project+Administration#ProjectAdministration-AddingaProject
        public static readonly StringComparer KeyComparer = StringComparer.Ordinal;

        public ProjectViewModel(ServerViewModel owner, ProjectInformation projectInformation)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (projectInformation == null)
            {
                throw new ArgumentNullException(nameof(projectInformation));
            }

            this.Owner = owner;
            this.ProjectInformation = projectInformation;
        }

        #region Properties
        public ServerViewModel Owner
        {
            get;
        }

        public ProjectInformation ProjectInformation
        {
            get;
        }

        public string Key
        {
            get { return this.ProjectInformation.Key; }
        }

        public string ProjectName
        {
            get { return this.ProjectInformation.Name; }
        }

        public bool IsBound
        {
            get { return this.isBound; }
            set { this.SetAndRaisePropertyChanged(ref this.isBound, value); }
        }

        public string ToolTipProjectName
        {
            get
            {
                return this.IsBound
                    ? string.Format(CultureInfo.CurrentCulture, Strings.ProjectToolTipProjectNameFormat, this.ProjectName)
                    : this.ProjectName;
            }
        }

        public string ToolTipKey
        {
            get
            {
                return string.Format(CultureInfo.CurrentCulture, Strings.ProjectToolTipKeyFormat, this.Key);
            }
        }

        public string AutomationName
        {
            get
            {
                return this.IsBound
                    ? string.Format(CultureInfo.CurrentCulture, Strings.AutomationProjectBoundDescription, this.ProjectName)
                    : this.ProjectName;
            }
        }

        #endregion

        #region Commands
        public ContextualCommandsCollection Commands
        {
            get { return this.commands; }
        }
        #endregion

    }
}
