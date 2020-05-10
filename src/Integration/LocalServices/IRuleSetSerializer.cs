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

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Abstraction for reading and writing of <see cref="RuleSet"/> instances.
    /// </summary>
    internal interface IRuleSetSerializer : ILocalService
    {
        /// <summary>
        /// Will write the specified <paramref name="ruleSet"/> into specified path.
        /// The caller needs to handler the various possible errors.
        /// </summary>
        void WriteRuleSetFile(RuleSet ruleSet, string path);

        /// <summary>
        /// Will load a RuleSet in specified <paramref name="path"/>.
        /// In case of error, null will be returned.
        /// </summary>
        RuleSet LoadRuleSet(string path);
    }
}
