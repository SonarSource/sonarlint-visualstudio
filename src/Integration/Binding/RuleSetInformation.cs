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
using SonarLint.VisualStudio.Core.Binding;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Data class that exposes simple data that can be accessed from any thread.
    /// The class itself is not thread safe and assumes only one thread accessing it at any given time.
    /// </summary>
    public class RuleSetInformation
    {
        public RuleSetInformation(Language language, IRulesConfigurationFile rulesConfigurationFile)
        {
            RulesConfigurationFile = rulesConfigurationFile ?? throw new ArgumentNullException(nameof(rulesConfigurationFile));
        }

        public IRulesConfigurationFile RulesConfigurationFile { get; }

        public string NewRuleSetFilePath { get; set; }
    }
}
