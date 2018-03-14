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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.CodeAnalysis.Extensibility;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    public interface IDeprecatedSonarRuleSetManager
    {
        void WarnIfAnyProjectHasSonarRuleSet();
    }

    [Export(typeof(IDeprecatedSonarRuleSetManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class DeprecatedSonarRuleSetManager : IDeprecatedSonarRuleSetManager
    {
        private readonly IProjectSystemHelper projectSystemHelper;
        private readonly ILogger logger;

        [ImportingConstructor]
        public DeprecatedSonarRuleSetManager(IHost host)
            : this(host.GetService<IProjectSystemHelper>(), host.Logger)
        {
        }

        internal DeprecatedSonarRuleSetManager(IProjectSystemHelper projectSystemHelper, ILogger logger)
        {
            if (projectSystemHelper == null)
            {
                throw new ArgumentNullException(nameof(projectSystemHelper));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.projectSystemHelper = projectSystemHelper;
            this.logger = logger;
        }

        public void WarnIfAnyProjectHasSonarRuleSet()
        {
            if (this.projectSystemHelper.GetSolutionProjects().Any(HasAnySonarRule))
            {
                this.logger.WriteLine(Strings.ProjectWithSonarRules);
            }
        }

        private bool HasAnySonarRule(EnvDTE.Project project)
        {
            // We only look for the "global" CodeAnalysisRuleSet property (i.e. not under any condition)
            var projectRuleSetPath = this.projectSystemHelper.GetProjectProperty(project, Constants.CodeAnalysisRuleSetPropertyKey);
            if (projectRuleSetPath == null)
            {
                return false;
            }

            var projectDirectoryFullPath = new FileInfo(project.FullName).Directory.FullName;
            var projectRuleSetFullPath = GetFullPath(projectRuleSetPath, projectDirectoryFullPath);
            if (!File.Exists(projectRuleSetFullPath))
            {
                return false;
            }

            var projectRuleSet = RuleSet.LoadFromFile(projectRuleSetFullPath);

            // 1. Collect all paths (current ruleset + includes)
            var ruleSetIncludeFullPaths = projectRuleSet.RuleSetIncludes
                .Select(include => GetFullPath(include.FilePath, projectDirectoryFullPath))
                .ToList();

            // 2. Look if any of the effective rules is from SonarAnalyzer and initialize dictionary with this result.
            return projectRuleSet
                .GetEffectiveRules(ruleSetIncludeFullPaths, new RuleInfoProvider[0])
                .Any(rule => rule.AnalyzerId.StartsWith("SonarAnalyzer.", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetFullPath(string maybeRelativePath, string relativeTo) =>
            Path.IsPathRooted(maybeRelativePath)
                ? maybeRelativePath
                : Path.GetFullPath(Path.Combine(relativeTo, maybeRelativePath));
    }
}
