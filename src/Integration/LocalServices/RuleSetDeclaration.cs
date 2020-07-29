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
using System.Linq;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Data-only class that represents rule set information for specific configuration
    /// </summary>
    public class RuleSetDeclaration
    {
        public RuleSetDeclaration(Project project, Property ruleSetProperty, string ruleSetPath, string activationContext, params string[] ruleSetDirectories)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (ruleSetProperty == null)
            {
                throw new ArgumentNullException(nameof(ruleSetProperty));
            }

            this.RuleSetProjectFullName = project.FullName;
            this.DeclaringProperty = ruleSetProperty;
            this.ConfigurationContext = activationContext;
            this.RuleSetPath = ruleSetPath;
            this.RuleSetDirectories = ruleSetDirectories.ToList(); // avoid aliasing bugs
        }

        /// <summary>
        /// <see cref="Project.FullName"/>
        /// </summary>
        public string RuleSetProjectFullName { get; }

        /// <summary>
        /// The property declaring the rule set
        /// </summary>
        public Property DeclaringProperty { get; }

        /// <summary>
        /// Path to the rule set file. File name, relative path, absolute path and whitespace are all valid.
        /// </summary>
        public string RuleSetPath { get; }

        /// <summary>
        /// Additional rule set directories to search the <see cref="RuleSetPath"/> i.e. in case the rule set is not an absolute path.
        /// </summary>
        public IEnumerable<string> RuleSetDirectories { get; }

        /// <summary>
        /// In which context the ruleset is active i.e. the configuration name
        /// </summary>
        public string ConfigurationContext { get; }

        public override string ToString()
        {
            return $"ConfigurationContext: {ConfigurationContext} | RuleSetPath: '{RuleSetPath}' | RuleSetDirectories: '{string.Join(";", RuleSetDirectories)}'";
        }
    }
}
