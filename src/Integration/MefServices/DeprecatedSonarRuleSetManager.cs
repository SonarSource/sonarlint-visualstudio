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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CodeAnalysis.Extensibility;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration
{
    public interface IDeprecatedSonarRuleSetManager : IDisposable
    {
    }

    [Export(typeof(IDeprecatedSonarRuleSetManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class DeprecatedSonarRuleSetManager : IDeprecatedSonarRuleSetManager
    {
        internal const string DeprecationMessage =
            "*****************************************************************************************\r\n" +
            "***   Some of the projects are using Sonar rules through the ruleset. This is not a   ***\r\n" +
            "***       supported configuration and any enabled rules that are not in SonarWay      ***\r\n" +
            "***   (standalone mode) or on the Quality Profile (connected mode) won't be enabled.  ***\r\n" +
            "*****************************************************************************************";

        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IProjectSystemHelper projectSystemHelper;
        private readonly ISolutionRuleSetsInformationProvider ruleSetProvider;
        private readonly ILogger logger;

        private bool isDisposed;

        [ImportingConstructor]
        public DeprecatedSonarRuleSetManager(IHost host)
            : this(host.GetMefService<IActiveSolutionBoundTracker>(), host.GetMefService<IActiveSolutionTracker>(),
                host.GetService<IProjectSystemHelper>(), host.GetService<ISolutionRuleSetsInformationProvider>(), host.Logger)
        {
        }

        internal /* for testing purposes */ DeprecatedSonarRuleSetManager(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IActiveSolutionTracker activeSolutionTracker, IProjectSystemHelper projectSystemHelper,
            ISolutionRuleSetsInformationProvider ruleSetProvider, ILogger logger)
        {
            if (activeSolutionBoundTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionBoundTracker));
            }

            if (activeSolutionTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionTracker));
            }

            if (projectSystemHelper == null)
            {
                throw new ArgumentNullException(nameof(projectSystemHelper));
            }

            if (ruleSetProvider == null)
            {
                throw new ArgumentNullException(nameof(ruleSetProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.activeSolutionTracker = activeSolutionTracker;
            this.projectSystemHelper = projectSystemHelper;
            this.ruleSetProvider = ruleSetProvider;
            this.logger = logger;

            this.activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
            this.activeSolutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;

            if (this.activeSolutionBoundTracker.CurrentConfiguration != null &&
                this.activeSolutionBoundTracker.CurrentConfiguration.Mode != NewConnectedMode.SonarLintMode.LegacyConnected)
            {
                WarnIfAnyProjectHasSonarRuleSet();
            }
        }

        private void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            try
            {
                if (e.IsSolutionOpen)
                {
                    WarnIfAnyProjectHasSonarRuleSet();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Swallow the exception as we are on UI thread
            }
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            try
            {
                if (e.Configuration != null &&
                    e.Configuration.Mode != NewConnectedMode.SonarLintMode.LegacyConnected)
                {
                    WarnIfAnyProjectHasSonarRuleSet();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Swallow the exception as we are on UI thread
            }
        }

        private void WarnIfAnyProjectHasSonarRuleSet()
        {
            var hasAnySonarRule = this.projectSystemHelper.GetSolutionProjects()
                .SelectMany(p => ruleSetProvider.GetProjectRuleSetsDeclarations(p))
                .Any(HasAnySonarRule);

            if (hasAnySonarRule)
            {
                this.logger.WriteLine(DeprecationMessage);
            }
        }

        private bool HasAnySonarRule(RuleSetDeclaration ruleSetDeclaration)
        {
            var projectDirectoryFullPath = Path.GetDirectoryName(ruleSetDeclaration.RuleSetProjectFullName);
            var projectRuleSetFullPath = GetFullPath(ruleSetDeclaration.RuleSetPath, projectDirectoryFullPath);
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

        public void Dispose()
        {
            if (!isDisposed)
            {
                this.activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
                this.activeSolutionTracker.ActiveSolutionChanged -= OnActiveSolutionChanged;
                isDisposed = true;
            }
        }
    }
}
