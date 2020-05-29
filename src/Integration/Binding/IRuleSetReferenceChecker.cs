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

using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal interface IRuleSetReferenceChecker
    {
        /// <summary>
        /// Return true if the project's ruleSets directly include the target ruleSet, otherwise false
        /// </summary>
        /// <remarks>We don't currently check nested includes in RuleSet
        /// i.e. if Project has RuleSet A which includes RuleSet B that includes RuleSet C, IsReferenced(Project, C) returns false.</remarks>
        bool IsReferenced(Project project, string targetRuleSetFilePath);
    }
}
