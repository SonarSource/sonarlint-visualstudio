/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal class SolutionRuleSetsInformationProvider : ISolutionRuleSetsInformationProvider
    {
        public const char RuleSetDirectoriesValueSpliter = ';';

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public SolutionRuleSetsInformationProvider(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, logger, new FileSystem())
        {
        }

        internal SolutionRuleSetsInformationProvider(IServiceProvider serviceProvider, ILogger logger, IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public IEnumerable<RuleSetDeclaration> GetProjectRuleSetsDeclarations(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            ETW.CodeMarkers.Instance.GetProjectRuleSetsDeclarationsStart(project.Name);

            var result = GetProjectRuleSetsDeclarationsIterator(project);

            ETW.CodeMarkers.Instance.GetProjectRuleSetsDeclarationsStop();

            return result;
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

            var declarations = new List<RuleSetDeclaration>();

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();

            // PERF: goldbar #2791. This is where most of the time is spent
            var ruleSetProperties = VsShellUtils.GetProjectProperties(project, Constants.CodeAnalysisRuleSetPropertyKey);
            Debug.Assert(ruleSetProperties != null);
            Debug.Assert(ruleSetProperties.All(p => p != null), "Not expecting nulls in the list of properties");

            if (!ruleSetProperties.Any())
            {
                logger.WriteLine(Strings.CouldNotFindCodeAnalysisRuleSetPropertyOnProject, project.UniqueName);
            }

            foreach (Property ruleSetProperty in ruleSetProperties)
            {
                string activationContext = TryGetPropertyConfiguration(ruleSetProperty)?.ConfigurationName ?? string.Empty;
                string ruleSetDirectoriesValue = projectSystem.GetProjectProperty(project, Constants.CodeAnalysisRuleSetDirectoriesPropertyKey, activationContext);
                string[] ruleSetDirectories = ruleSetDirectoriesValue?.Split(new[] { RuleSetDirectoriesValueSpliter }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                string ruleSetValue = ruleSetProperty.Value as string;

                var declaration = new RuleSetDeclaration(project, ruleSetProperty, ruleSetValue, activationContext, ruleSetDirectories);
                declarations.Add(declaration);
            }

            return declarations;
        }

        public bool TryGetProjectRuleSetFilePath(RuleSetDeclaration declaration, out string fullFilePath)
        {
            if (string.IsNullOrWhiteSpace(declaration.RuleSetPath))
            {
                fullFilePath = null;
                return false;
            }

            var options = new List<string>();
            options.Add(declaration.RuleSetPath); // Might be a full path
            options.Add(PathHelper.ResolveRelativePath(declaration.RuleSetPath, declaration.RuleSetProjectFullName)); // Relative to project
            // Note: currently we don't search in rule set directories since we expect the project rule set
            // to be relative to the project. We can add this in the future if it will be needed.

            fullFilePath = options.FirstOrDefault(fileSystem.File.Exists);

            return !string.IsNullOrWhiteSpace(fullFilePath);
        }

        private static Configuration TryGetPropertyConfiguration(Property property)
        {
            Configuration configuration = property.Collection.Parent as Configuration; // Could be null if the one used is the Project level one.
            Debug.Assert(configuration != null || property.Collection.Parent is Project, $"Unexpected property parent type: {property.Collection.Parent.GetType().FullName}");
            return configuration;
        }
    }
}
