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

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that represents aggregated rule set information.
    /// Same rule sets should be represented by the same instance of <see cref="RuleSetInformation"/> and have their <see cref="RuleSetDeclaration.ConfigurationContext"/>
    /// associated with the shared instance by adding them into <see cref="ConfigurationContexts"/>.
    /// </summary>
    /// <seealso cref="RuleSetDeclaration"/>
    public class RuleSetInformation
    {
        public RuleSetInformation(string projectFullName, string baselineRuleSet, string projectRuleSet, IEnumerable<string> ruleSetDirectories)
        {
            if (string.IsNullOrWhiteSpace(projectFullName))
            {
                throw new ArgumentNullException(nameof(projectFullName));
            }

            if (string.IsNullOrWhiteSpace(baselineRuleSet))
            {
                throw new ArgumentNullException(nameof(baselineRuleSet));
            }

            if (string.IsNullOrWhiteSpace(projectRuleSet))
            {
                throw new ArgumentNullException(nameof(projectRuleSet));
            }

            this.RuleSetProjectFullName = projectFullName;
            this.BaselineFilePath = baselineRuleSet;
            this.RuleSetFilePath = projectRuleSet;
            this.RuleSetDirectories = ruleSetDirectories?.ToArray() ?? new string[0];
        }

        public string RuleSetProjectFullName { get; }

        public string BaselineFilePath { get; }

        public string RuleSetFilePath { get; }

        public string[] RuleSetDirectories { get; }

        public HashSet<string> ConfigurationContexts { get; } = new HashSet<string>();
    }
}
