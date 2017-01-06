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
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    interface ISolutionRuleSetsInformationProvider : ILocalService
    {
        /// <summary>
        /// For a given <paramref name="project"/> will return all the <see cref="RuleSetDeclaration"/>
        /// </summary>
        /// <param name="project">Required</param>
        /// <returns>Not null</returns>
        IEnumerable<RuleSetDeclaration> GetProjectRuleSetsDeclarations(Project project);

        /// <summary>
        /// Will return a calculate file path to the shared SonarQube RuleSet
        /// that corresponds to the <paramref name="sonarQubeProjectKey"/> (with  <paramref name="fileNameSuffix"/>).
        /// </summary>
        /// <param name="sonarQubeProjectKey">Required</param>
        /// <param name="language">The language this rule set corresponds to</param>
        /// <returns>Full file path. The file may not actually exist on disk</returns>
        string CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, Language language);

        /// <summary>
        /// Will return a calculated file path to the expected project RuleSet
        /// that corresponds to the <paramref name="declaration"/>.
        /// </summary>
        /// <param name="project">Required</param>
        /// <param name="declaration">Required</param>
        /// <param name="fullFilePath">A full file path to an existing RuleSet, or null if failed.</param>
        /// <returns>Whether succeeded in which case the <param name="fullFilePath" /> will point to an existing file</returns>
        bool TryGetProjectRuleSetFilePath(Project project, RuleSetDeclaration declaration, out string fullFilePath);

        /// <summary>
        /// Returns the path to solution level rulesets.
        /// When the solution is closed returns null.
        /// </summary>
        string GetSolutionSonarQubeRulesFolder();
    }
}
