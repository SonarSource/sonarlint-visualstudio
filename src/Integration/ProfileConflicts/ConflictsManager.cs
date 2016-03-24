//-----------------------------------------------------------------------
// <copyright file="ConflictsManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Conflicts manager for a SonarQube bound solution
    /// </summary>
    internal class ConflictsManager : IConflictsManager
    {
        private readonly IServiceProvider serviceProvider;

        public ConflictsManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        public IReadOnlyList<ProjectRuleSetConflict> GetCurrentConflicts()
        {
            // Note: some of the assumptions (see asserts below) are because have just bounded the solution (as documented on the interface),
            // in other cases assuming that the rule set are indeed on disk is not possible, and in fact resyncing 
            // would be required when we have missing rulesets, otherwise finding conflicts will not be possible.

            RuleSetAggregate[] aggregatedRuleSets = GetAggregatedSolutionRuleSets();

            if (aggregatedRuleSets.Length > 0)
            {
                return FindConflicts(aggregatedRuleSets);
            }

            return new ProjectRuleSetConflict[0];
        }

        private IReadOnlyList<ProjectRuleSetConflict> FindConflicts(RuleSetAggregate[] aggregatedRuleSet)
        {
            List<ProjectRuleSetConflict> conflicts = new List<ProjectRuleSetConflict>();

            IRuleSetInspector inspector = this.serviceProvider.GetService<IRuleSetInspector>();
            inspector.AssertLocalServiceIsNotNull();

            // At the moment single threaded, if needed this could be done in parallel
            foreach (RuleSetAggregate aggregate in aggregatedRuleSet)
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

        private RuleSetAggregate[] GetAggregatedSolutionRuleSets()
        {
            var solutionBinding = this.serviceProvider.GetService<ISolutionBinding>();
            solutionBinding.AssertLocalServiceIsNotNull();

            BoundSonarQubeProject bindingInfo = solutionBinding.ReadSolutionBinding();
            if (bindingInfo != null)
            {
                var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
                projectSystem.AssertLocalServiceIsNotNull();

                var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
                ruleSetInfoProvider.AssertLocalServiceIsNotNull();

                var fileSystem = this.serviceProvider.GetService<IFileSystem>();
                fileSystem.AssertLocalServiceIsNotNull();

                var projectRuleSetAggregation = new Dictionary<string, RuleSetAggregate>(StringComparer.OrdinalIgnoreCase);

                foreach (Project project in projectSystem.GetSolutionManagedProjects())
                {
                    string suffix = SolutionBindingOperation.GetProjectRuleSetSuffix(ProjectBindingOperation.GetProjectGroup(project));
                    string baselineRuleSet = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(bindingInfo.ProjectKey, suffix);

                    if (!fileSystem.FileExist(baselineRuleSet))
                    {
                        this.WriteWarning(Strings.ExpectedRuleSetNotFound, baselineRuleSet, project.FullName);
                        continue;
                    }

                    foreach (RuleSetDeclaration declaration in ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project))
                    {
                        string projectRuleSet = CalculateProjectRuleSetFullPath(fileSystem, project, declaration);

                        if (string.IsNullOrWhiteSpace(projectRuleSet))
                        {
                            // Use the original property value to attempt to load the rule set with the directories information, during FindConflicts
                            projectRuleSet = declaration.RuleSetPath;

                            // We do want to continue and add this rule set rather than skip over it since it maybe the case that
                            // the user moved it to some other location which is relative to the rule set directories property
                            // and we will be able to find it during rule set conflicts analysis.
                        }

                        RuleSetAggregate aggregate;
                        if (projectRuleSetAggregation.TryGetValue(projectRuleSet, out aggregate))
                        {
                            aggregate.ActivationContexts.Add(declaration.ActivationContext);

                            if (!aggregate.RuleSetDirectories.SequenceEqual(declaration.RuleSetDirectories))
                            {
                                this.WriteWarning(Strings.InconsistedRuleSetDirectoriesWarning,
                                    CombineDirectories(declaration.RuleSetDirectories),
                                    projectRuleSet,
                                    CombineDirectories(aggregate.RuleSetDirectories));
                            }
                        }
                        else
                        {
                            aggregate = new RuleSetAggregate(declaration.RuleSetProjectFullName, baselineRuleSet, projectRuleSet, declaration.RuleSetDirectories);
                            aggregate.ActivationContexts.Add(declaration.ActivationContext);
                            projectRuleSetAggregation[projectRuleSet] = aggregate;
                        }
                    }
                }

                return projectRuleSetAggregation.Values.ToArray();
            }

            return new RuleSetAggregate[0];
        }

        private void WriteWarning(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, format, args);
            VsShellUtils.WriteToGeneralOutputPane(this.serviceProvider, Strings.ConflictsManagerWarningMessage, message);
        }

        private static string CalculateProjectRuleSetFullPath(IFileSystem fileSystem, Project project, RuleSetDeclaration declaration)
        {
            List<string> options = new List<string>();
            options.Add(declaration.RuleSetPath); // Might be a full path
            options.Add(PathHelper.ResolveRelativePath(declaration.RuleSetPath, project.FullName)); // Relative to project

            // Note at this stage we don't care about rule set directories, since we expect that 
            // in worst case to get an exception from the rule inspector when it will try to load the rule set

            return options.FirstOrDefault(fileSystem.FileExist);
        }

        internal /*for testing purposes*/ static string CombineDirectories(IEnumerable<string> directories)
        {
            return string.Join(";", directories);
        }
    }
}
