﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Collections.Generic;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.CSharpVB
{
    public interface IRuleSetGenerator
    {
        /// <summary>
        /// Generates a RuleSet that is serializable (XML).
        /// The ruleset can be empty if there are no active rules belonging to the repo keys "vbnet", "csharpsquid" or "roslyn.*".
        /// </summary>
        /// <exception cref="InvalidOperationException">if required properties that should be associated with the repo key are missing.</exception>
        RuleSet Generate(string language, IEnumerable<SonarQubeRule> rules, IDictionary<string, string> sonarProperties);
    }
}
