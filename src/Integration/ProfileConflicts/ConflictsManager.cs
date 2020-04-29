/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    // Legacy connected mode:
    // This class detects and returns rulesets where the locally configured
    // ruleset is missing, or is weaker than the ruleset that auto-generated
    // from the Quality Profile.

    /// <summary>
    /// Conflicts manager for a SonarQube bound solution
    /// </summary>
    internal class ConflictsManager : IConflictsManager
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IProjectBinderFactory projectBinderFactory;
        private readonly IFileSystem fileSystem;

        public ConflictsManager(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, logger, new ProjectBinderFactory(serviceProvider), new FileSystem())
        {
        }

        internal ConflictsManager(IServiceProvider serviceProvider, ILogger logger, IProjectBinderFactory projectBinderFactory, IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.projectBinderFactory = projectBinderFactory ?? throw new ArgumentNullException(nameof(projectBinderFactory));
        }

        public IReadOnlyList<ProjectRuleSetConflict> GetCurrentConflicts()
        {
            var configProvider = this.serviceProvider.GetService<IConfigurationProvider>();
            configProvider.AssertLocalServiceIsNotNull();

            // Check that we are in legacy connected mode
            var bindingConfig = configProvider.GetConfiguration();
            if (bindingConfig.Mode != SonarLintMode.LegacyConnected)
            {
                return new ProjectRuleSetConflict[0];
            }
            Debug.Assert(bindingConfig.Project != null, "Bound project should not be null if in legacy connected mode");

            // Note: some of the assumptions (see asserts below) are because have just bounded the solution (as documented on the interface),
            // in other cases assuming that the rule set are indeed on disk is not possible, and in fact re-syncing
            // would be required when we have missing rule-sets, otherwise finding conflicts will not be possible.

            RuleSetInformation[] aggregatedRuleSets = CheckSlnLevelConfigExistsAndReturnAllProjectRuleSetsForAllConfigurations(bindingConfig.Project);

            if (aggregatedRuleSets.Length > 0)
            {
                return FindConflicts(aggregatedRuleSets);
            }

            return new ProjectRuleSetConflict[0];
        }

        private IReadOnlyList<ProjectRuleSetConflict> FindConflicts(RuleSetInformation[] aggregatedRuleSet)
        {
            List<ProjectRuleSetConflict> conflicts = new List<ProjectRuleSetConflict>();

            IRuleSetInspector inspector = this.serviceProvider.GetService<IRuleSetInspector>();
            inspector.AssertLocalServiceIsNotNull();

            // At the moment single threaded, if needed this could be done in parallel
            foreach (RuleSetInformation aggregate in aggregatedRuleSet)
            {
                try
                {
                    RuleConflictInfo conflict = inspector.FindConflictingRules(aggregate.BaselineFilePath, aggregate.RuleSetFilePath, aggregate.RuleSetDirectories);
                    Debug.Assert(conflict != null);

                    if (conflict?.HasConflicts ?? false)
                    {
                        conflicts.Add(new ProjectRuleSetConflict(conflict, aggregate));
                    }
                }
                catch (Exception ex)
                {
                    if (ErrorHandler.IsCriticalException(ex))
                    {
                        throw;
                    }

                    this.WriteWarning(Strings.ConflictsManagerFailedInFindConflicts, aggregate.RuleSetFilePath, aggregate.BaselineFilePath, ex.Message);
                    Debug.Fail("Failed to resolve conflict for " + aggregate.RuleSetFilePath, ex.ToString());
                }
            }

            return conflicts;
        }

        // ISSUE : this method is doing too many things
        private RuleSetInformation[] CheckSlnLevelConfigExistsAndReturnAllProjectRuleSetsForAllConfigurations(
            BoundSonarQubeProject bindingInfo)
        {
            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            var projectRuleSetAggregation = new Dictionary<string, RuleSetInformation>(StringComparer.OrdinalIgnoreCase);

            foreach (Project project in projectSystem.GetFilteredSolutionProjects())
            {
                // Solution-level checks (done here because the expected solution-level config
                // depends on the languages supported by the project that exist)
                string baselineRuleSet = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(
                    bindingInfo.ProjectKey,
                    ProjectToLanguageMapper.GetLanguageForProject(project),
                    SonarLintMode.LegacyConnected);

                if (!fileSystem.File.Exists(baselineRuleSet))
                {
                    this.WriteWarning(Strings.ExpectedRuleSetNotFound, baselineRuleSet, project.FullName);
                    continue;
                }

                if (projectBinderFactory.Get(project) is RoslynProjectBinder)
                {
                    foreach (var declaration in ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project))
                    {
                        string projectRuleSet =
                            CalculateProjectRuleSetFullPath(ruleSetInfoProvider, project, declaration);

                        this.AddOrUpdateAggregatedRuleSetInformation(projectRuleSetAggregation, baselineRuleSet,
                            declaration, projectRuleSet);
                    }
                }
            }

            return projectRuleSetAggregation.Values.ToArray();
        }

        private void AddOrUpdateAggregatedRuleSetInformation(Dictionary<string, RuleSetInformation> projectRuleSetAggregation, string baselineRuleSet, RuleSetDeclaration declaration, string projectRuleSet)
        {
            RuleSetInformation aggregate;
            if (projectRuleSetAggregation.TryGetValue(projectRuleSet, out aggregate))
            {
                aggregate.ConfigurationContexts.Add(declaration.ConfigurationContext);

                if (!aggregate.RuleSetDirectories.SequenceEqual(declaration.RuleSetDirectories))
                {
                    this.WriteWarning(Strings.InconsistentRuleSetDirectoriesWarning,
                                    CombineDirectories(declaration.RuleSetDirectories),
                                    projectRuleSet,
                                    CombineDirectories(aggregate.RuleSetDirectories));
                }
            }
            else
            {
                aggregate = new RuleSetInformation(declaration.RuleSetProjectFullName, baselineRuleSet, projectRuleSet, declaration.RuleSetDirectories);
                aggregate.ConfigurationContexts.Add(declaration.ConfigurationContext);
                projectRuleSetAggregation[projectRuleSet] = aggregate;
            }
        }

        private void WriteWarning(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, format, args);
            logger.WriteLine(Strings.ConflictsManagerWarningMessage, message);
        }

        private static string CalculateProjectRuleSetFullPath(ISolutionRuleSetsInformationProvider ruleSetInfoProvider, Project project, RuleSetDeclaration declaration)
        {
            string projectRuleSet;

            if (!ruleSetInfoProvider.TryGetProjectRuleSetFilePath(project, declaration, out projectRuleSet))
            {
                // Use the original property value to attempt to load the rule set with the directories information, during FindConflicts
                projectRuleSet = declaration.RuleSetPath;

                // We do want to continue and add this rule set rather than skip over it since it maybe the case that
                // the user moved it to some other location which is relative to the rule set directories property
                // and we will be able to find it during rule set conflicts analysis.
            }

            return projectRuleSet;
        }

        internal /*for testing purposes*/ static string CombineDirectories(IEnumerable<string> directories)
        {
            return string.Join(";", directories);
        }
    }
}
