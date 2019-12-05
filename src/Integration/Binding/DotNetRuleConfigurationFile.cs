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
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class DotNetRulesConfigurationFile : IRulesConfigurationFileWithRuleset
    {
        public DotNetRulesConfigurationFile(RuleSet ruleSet)
        {
            this.RuleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
        }

        #region IRulesConfigurationFileWithRuleset methods

        public RuleSet RuleSet { get; }

        public void Save(string fullFilePath)
        {
            if (string.IsNullOrWhiteSpace(fullFilePath))
            {
                throw new ArgumentNullException(nameof(fullFilePath));
            }

            this.RuleSet.WriteToFile(fullFilePath);
        }

        #endregion
    }
}
