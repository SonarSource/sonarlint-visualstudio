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

namespace SonarLint.VisualStudio.Integration
{
    public static class Constants
    {
        /// <summary>
        /// The property key which corresponds to the code analysis rule set
        /// </summary>
        public const string CodeAnalysisRuleSetPropertyKey = "CodeAnalysisRuleSet";

        /// <summary>
        /// The property key which corresponds to the code analysis rule set directories
        /// </summary>
        public const string CodeAnalysisRuleSetDirectoriesPropertyKey = "CodeAnalysisRuleSetDirectories";

        /// <summary>
        /// The directory name of the SonarQube specific files that are being created
        /// </summary>
        public const string SonarQubeManagedFolderName = "SonarQube";

        /// <summary>
        /// The generated rule set name
        /// </summary>
        public const string RuleSetName = "SonarQube";

        /// <summary>
        /// The property key which corresponds to the Roslyn analyzer additional files
        /// </summary>
        public const string AdditionalFilesItemTypeName = "AdditionalFiles";

        /// <summary>
        /// The SonarQube home page
        /// </summary>
        public const string SonarQubeHomeWebUrl = "http://sonarqube.org";

        /// <summary>
        /// SonarLint issues home page
        /// </summary>
        public const string SonarLintIssuesWebUrl = "https://groups.google.com/forum/#!forum/sonarlint";

        /// <summary>
        /// The property key which corresponds to the ItemType of a <see cref="EnvDTE.ProjectItem"/>.
        /// </summary>
        public const string ItemTypePropertyKey = "ItemType";

        /// <summary>
        /// Ruleset file extension
        /// </summary>
        public const string RuleSetFileExtension = "ruleset";

        /// <summary>
        /// The build property key which corresponds to the explicit SonarQube project exclusion.
        /// </summary>
        public const string SonarQubeExcludeBuildPropertyKey = "SonarQubeExclude";

        /// <summary>
        /// The build property key which corresponds to the explicit SonarQube test project identification.
        /// </summary>
        public const string SonarQubeTestProjectBuildPropertyKey = "SonarQubeTestProject";

    }
}
