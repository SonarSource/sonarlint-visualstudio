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
using System.IO;
using EnvDTE;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableSolutionRuleSetsInformationProvider : ISolutionRuleSetsInformationProvider
    {
        private readonly Dictionary<Project, List<RuleSetDeclaration>> registeredProjectData = new Dictionary<Project, List<RuleSetDeclaration>>();

        #region ISolutionRuleSetsInformationProvider

        IEnumerable<RuleSetDeclaration> ISolutionRuleSetsInformationProvider.GetProjectRuleSetsDeclarations(Project project)
        {
            project.Should().NotBeNull();

            List<RuleSetDeclaration> result;
            if (!this.registeredProjectData.TryGetValue(project, out result))
            {
                result = new List<RuleSetDeclaration>();
                this.registeredProjectData[project] = result;
            }

            return result;
        }

        string ISolutionRuleSetsInformationProvider.GetSolutionSonarQubeRulesFolder()
        {
            return Path.Combine(this.SolutionRootFolder, Constants.SonarQubeManagedFolderName);
        }

        string ISolutionRuleSetsInformationProvider.CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, Language language)
        {
            string fileName = $"{sonarQubeProjectKey}{language.Id}.{Constants.RuleSetFileExtension}";
            return Path.Combine(((ISolutionRuleSetsInformationProvider)this).GetSolutionSonarQubeRulesFolder(), fileName);
        }

        bool ISolutionRuleSetsInformationProvider.TryGetProjectRuleSetFilePath(Project project, RuleSetDeclaration declaration, out string fullFilePath)
        {
            fullFilePath = declaration.RuleSetPath;

            return true;
        }

        #endregion ISolutionRuleSetsInformationProvider

        #region Test helpers

        public void RegisterProjectInfo(Project project, params RuleSetDeclaration[] info)
        {
            List<RuleSetDeclaration> declarations;

            if (!this.registeredProjectData.TryGetValue(project, out declarations))
            {
                declarations = new List<RuleSetDeclaration>();
                this.registeredProjectData[project] = declarations;
            }

            declarations.AddRange(info);
        }

        public void ClearProjectInfo(Project project)
        {
            this.registeredProjectData.Remove(project);
        }

        public string SolutionRootFolder { get; set; }

        #endregion Test helpers
    }
}