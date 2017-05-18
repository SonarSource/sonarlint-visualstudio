/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal class SolutionRuleSetsInformationProvider : ISolutionRuleSetsInformationProvider
    {
        public const char RuleSetDirectoriesValueSpliter = ';';

        private readonly IServiceProvider serviceProvider;

        public SolutionRuleSetsInformationProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        public IEnumerable<RuleSetDeclaration> GetProjectRuleSetsDeclarations(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            bool found = false;

            foreach (Property ruleSetProperty in VsShellUtils.EnumerateProjectProperties(project, Constants.CodeAnalysisRuleSetPropertyKey).Where(p => p != null))
            {
                found = true;

                string ruleSetDirectoriesValue = VsShellUtils.FindProperty(ruleSetProperty.Collection, Constants.CodeAnalysisRuleSetDirectoriesPropertyKey)?.Value as string;
                string[] ruleSetDirectories = ruleSetDirectoriesValue?.Split(new[] { RuleSetDirectoriesValueSpliter }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                string ruleSetValue = ruleSetProperty.Value as string;
                string activationContext = TryGetPropertyConfiguration(ruleSetProperty)?.ConfigurationName ?? string.Empty;

                yield return new RuleSetDeclaration(project, ruleSetProperty, ruleSetValue, activationContext, ruleSetDirectories);
            }

            if (!found)
            {
                VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.CouldNotFindCodeAnalysisRuleSetPropertyOnProject, project.UniqueName);
            }
        }

        public string GetSolutionSonarQubeRulesFolder()
        {
            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            string solutionFullPath = projectSystem.GetCurrentActiveSolution()?.FullName;

            // Solution closed?
            if (string.IsNullOrWhiteSpace(solutionFullPath))
            {
                return null;
            }

            string solutionRoot = Path.GetDirectoryName(solutionFullPath);
            string ruleSetDirectoryRoot = Path.Combine(solutionRoot, Constants.SonarQubeManagedFolderName);

            return ruleSetDirectoryRoot;
        }

        public string CalculateSolutionSonarQubeRuleSetFilePath(string sonarQubeProjectKey, Language language)
        {
            if (string.IsNullOrWhiteSpace(sonarQubeProjectKey))
            {
                throw new ArgumentNullException(nameof(sonarQubeProjectKey));
            }

            string ruleSetDirectoryRoot = this.GetSolutionSonarQubeRulesFolder();

            if (string.IsNullOrWhiteSpace(ruleSetDirectoryRoot))
            {
                throw new InvalidOperationException(Strings.SolutionIsClosed);
            }

            string fileNameSuffix = language.Id;
            return GenerateSolutionRuleSetPath(ruleSetDirectoryRoot, sonarQubeProjectKey, fileNameSuffix);
        }

        public bool TryGetProjectRuleSetFilePath(Project project, RuleSetDeclaration declaration, out string fullFilePath)
        {
            List<string> options = new List<string>();
            options.Add(declaration.RuleSetPath); // Might be a full path
            options.Add(PathHelper.ResolveRelativePath(declaration.RuleSetPath, project.FullName)); // Relative to project
            // Note: currently we don't search in rule set directories since we expect the project rule set
            // to be relative to the project. We can add this in the future if it will be needed.

            IFileSystem fileSystem = this.serviceProvider.GetService<IFileSystem>();
            fileSystem.AssertLocalServiceIsNotNull();

            fullFilePath = options.FirstOrDefault(fileSystem.FileExist);

            return !string.IsNullOrWhiteSpace(fullFilePath);
        }

        private static Configuration TryGetPropertyConfiguration(Property property)
        {
            Configuration configuration = property.Collection.Parent as Configuration; // Could be null if the one used is the Project level one.
            Debug.Assert(configuration != null || property.Collection.Parent is Project, $"Unexpected property parent type: {property.Collection.Parent.GetType().FullName}");
            return configuration;
        }

        /// <summary>
        /// Generate a solution level rule set file path base on <paramref name="sonarQubeProjectKey"/> and <see cref="fileNameSuffix"/>
        /// </summary>
        /// <param name="ruleSetRootPath">Root directory to generate the full file path under</param>
        /// <param name="sonarQubeProjectKey">SonarQube project key to generate a rule set file name path for</param>
        /// <param name="fileNameSuffix">Fixed file name suffix</param>
        private static string GenerateSolutionRuleSetPath(string ruleSetRootPath, string sonarQubeProjectKey, string fileNameSuffix)
        {
            // Cannot use Path.ChangeExtension here because if the sonar project name contains
            // a dot (.) then everything after this will be replaced with .ruleset
            string fileName = $"{PathHelper.EscapeFileName(sonarQubeProjectKey + fileNameSuffix)}.{Constants.RuleSetFileExtension}";
            return Path.Combine(ruleSetRootPath, fileName);
        }
    }
}
