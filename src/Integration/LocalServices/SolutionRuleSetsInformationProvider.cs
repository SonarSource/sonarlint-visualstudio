/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal class SolutionRuleSetsInformationProvider : ISolutionRuleSetsInformationProvider
    {
        public const char RuleSetDirectoriesValueSpliter = ';';

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;

        public SolutionRuleSetsInformationProvider(IServiceProvider serviceProvider, ILogger logger)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public IEnumerable<RuleSetDeclaration> GetProjectRuleSetsDeclarations(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return GetProjectRuleSetsDeclarationsIterator(project);
        }

        private IEnumerable<RuleSetDeclaration> GetProjectRuleSetsDeclarationsIterator(Project project)
        {
            /* This method walks through all of the available configurations (e.g. Debug, Release, Foo) and
             * attempts to fetch the values of a couple of properties from the project (CodeAnalysisRuleSet
             * and CodeAnalysisRuleSetDirectories). The collected data is put into a data object
             * and returned to the caller. The collected data includes the DTE Property object itself, which
             * is used later to update the ruleset value.
             *
             * TODO: consider refactoring. The code seems over-complicated: it finds the "ruleset"
             * property for all configurations, then backtracks to find the configuration, then looks
             * for the corresponding "ruleset directories" property.
             * Note: we are now fetching the "ruleset directories" property from the MSBuild project,
             * rather than through the DTE (the previous version of this code that used the DTE fails
             * for C# and VB projects that use the new project system).
             */

            bool found = false;

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();

            var ruleSetProperties = VsShellUtils.GetProjectProperties(project, Constants.CodeAnalysisRuleSetPropertyKey);
            Debug.Assert(ruleSetProperties != null);
            Debug.Assert(ruleSetProperties.All(p => p != null), "Not expecting nulls in the list of properties");
            foreach (Property ruleSetProperty in ruleSetProperties)
            {
                found = true;

                string activationContext = TryGetPropertyConfiguration(ruleSetProperty)?.ConfigurationName ?? string.Empty;
                string ruleSetDirectoriesValue = projectSystem.GetProjectProperty(project, Constants.CodeAnalysisRuleSetDirectoriesPropertyKey, activationContext);
                string[] ruleSetDirectories = ruleSetDirectoriesValue?.Split(new[] { RuleSetDirectoriesValueSpliter }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                string ruleSetValue = ruleSetProperty.Value as string;

                yield return new RuleSetDeclaration(project, ruleSetProperty, ruleSetValue, activationContext, ruleSetDirectories);
            }

            if (!found)
            {
                logger.WriteLine(Strings.CouldNotFindCodeAnalysisRuleSetPropertyOnProject, project.UniqueName);
            }
        }

        public string GetSolutionSonarQubeRulesFolder(SonarLintMode bindingMode)
        {
            bindingMode.ThrowIfNotConnected();

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            string solutionFullPath = projectSystem.GetCurrentActiveSolution()?.FullName;

            // Solution closed?
            if (string.IsNullOrWhiteSpace(solutionFullPath))
            {
                return null;
            }

            string solutionRoot = Path.GetDirectoryName(solutionFullPath);
            string ruleSetDirectoryRoot = Path.Combine(solutionRoot,
                bindingMode == SonarLintMode.LegacyConnected ?
                Constants.LegacySonarQubeManagedFolderName :
                Constants.SonarlintManagedFolderName);

            return ruleSetDirectoryRoot;
        }

        public string CalculateSolutionSonarQubeRuleSetFilePath(string ProjectKey, Language language, SonarLintMode bindingMode)
        {
            if (string.IsNullOrWhiteSpace(ProjectKey))
            {
                throw new ArgumentNullException(nameof(ProjectKey));
            }

            if (language == null)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            bindingMode.ThrowIfNotConnected();

            string ruleSetDirectoryRoot = this.GetSolutionSonarQubeRulesFolder(bindingMode);

            if (string.IsNullOrWhiteSpace(ruleSetDirectoryRoot))
            {
                throw new InvalidOperationException(Strings.SolutionIsClosed);
            }

            string fileNameSuffix = language.Id;
            return GenerateSolutionRuleSetPath(ruleSetDirectoryRoot, ProjectKey, fileNameSuffix);
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
        /// Generate a solution level rule set file path base on <paramref name="ProjectKey"/> and <see cref="fileNameSuffix"/>
        /// </summary>
        /// <param name="ruleSetRootPath">Root directory to generate the full file path under</param>
        /// <param name="ProjectKey">SonarQube project key to generate a rule set file name path for</param>
        /// <param name="fileNameSuffix">Fixed file name suffix</param>
        private static string GenerateSolutionRuleSetPath(string ruleSetRootPath, string ProjectKey, string fileNameSuffix)
        {
            // Cannot use Path.ChangeExtension here because if the sonar project name contains
            // a dot (.) then everything after this will be replaced with .ruleset
            string fileName = $"{PathHelper.EscapeFileName(ProjectKey + fileNameSuffix)}.{Constants.RuleSetFileExtension}";
            return Path.Combine(ruleSetRootPath, fileName);
        }
    }
}
