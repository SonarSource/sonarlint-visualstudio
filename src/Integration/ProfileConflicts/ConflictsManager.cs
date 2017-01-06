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
            // in other cases assuming that the rule set are indeed on disk is not possible, and in fact re-syncing
            // would be required when we have missing rule-sets, otherwise finding conflicts will not be possible.

            RuleSetInformation[] aggregatedRuleSets = GetAggregatedSolutionRuleSets();

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

        private RuleSetInformation[] GetAggregatedSolutionRuleSets()
        {
            var solutionBinding = this.serviceProvider.GetService<ISolutionBindingSerializer>();
            solutionBinding.AssertLocalServiceIsNotNull();

            BoundSonarQubeProject bindingInfo = solutionBinding.ReadSolutionBinding();
            if (bindingInfo == null)
            {
                return new RuleSetInformation[0];
            }

            var projectSystem = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            var ruleSetInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();

            var fileSystem = this.serviceProvider.GetService<IFileSystem>();
            fileSystem.AssertLocalServiceIsNotNull();

            var projectRuleSetAggregation = new Dictionary<string, RuleSetInformation>(StringComparer.OrdinalIgnoreCase);

            foreach (Project project in projectSystem.GetFilteredSolutionProjects())
            {
                string baselineRuleSet = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(
                    bindingInfo.ProjectKey,
                    Language.ForProject(project));

                if (!fileSystem.FileExist(baselineRuleSet))
                {
                    this.WriteWarning(Strings.ExpectedRuleSetNotFound, baselineRuleSet, project.FullName);
                    continue;
                }

                foreach (RuleSetDeclaration declaration in ruleSetInfoProvider.GetProjectRuleSetsDeclarations(project))
                {
                    string projectRuleSet = CalculateProjectRuleSetFullPath(ruleSetInfoProvider, project, declaration);

                    this.AddOrUpdateAggregatedRuleSetInformation(projectRuleSetAggregation, baselineRuleSet, declaration, projectRuleSet);
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
            VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.ConflictsManagerWarningMessage, message);
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
