/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    public interface ISuppressionExecutionContext
    {
        bool IsInConnectedMode { get; }
        string SettingsKey { get; }
    }
    internal class SuppressionExecutionContext : ISuppressionExecutionContext
    {
        private const string Exp = @"\\SonarLint for Visual Studio\\Bindings\\(?<solutionName>[^\\/]+)\\(CSharp|VB)\\SonarLint.xml$";
        private static readonly Regex SonarLintFileRegEx = new Regex(Exp,
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant,
            RegexConstants.DefaultTimeout);

        public SuppressionExecutionContext(AnalyzerOptions analyzerOptions)
        {
            GetProjectKey(analyzerOptions);
        }

        private void GetProjectKey(AnalyzerOptions analyzerOptions)
        {
            foreach (var item in analyzerOptions.AdditionalFiles)
            {
                var match = SonarLintFileRegEx.Match(item.Path);
                if (match.Success)
                {
                    SettingsKey = match.Groups["solutionName"].Value;
                    return;
                }
            }
        }

        public bool IsInConnectedMode => SettingsKey != null;

        public string SettingsKey { get; private set; } = null;

        public string Mode => IsInConnectedMode ? "Connected" : "Standalone";
    }
}
