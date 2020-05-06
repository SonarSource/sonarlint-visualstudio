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
using SonarLint.VisualStudio.Core.Binding;

// Note: this interface was added as part of the refactoring that was done when
// the support for configuration of C++ files in Connected Mode was added.
// It minimised the changes required to the existing binding code that is
// ruleset-specific, at the cost of downcasts in a couple of places (done by
// the TryGetRuleSet extension method).

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Extends the base binding configuration interface for C#/VB projects where
    /// the config is expected to have a ruleset
    /// </summary>
    public interface IBindingConfigFileWithRuleset : IBindingConfigFile
    {
        RuleSet RuleSet { get; }
    }
}
