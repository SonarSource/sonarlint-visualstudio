/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    public interface ISuppressionExecutionContext
    {
        bool IsInConnectedMode { get; }
        string SonarProjectKey { get; }
    }
    internal class SuppressionExecutionContext : ISuppressionExecutionContext
    {
        private const string Exp = @"\\.sonarlint\\(?<sonarkey>[^\\/]+)\\(CSharp|VB)\\SonarLint.xml$";
        private static readonly Regex SonarLintFileRegEx = new Regex(Exp, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private readonly AnalyzerOptions analyzerOptions;


        public SuppressionExecutionContext(AnalyzerOptions analyzerOptions)
        {
            this.analyzerOptions = analyzerOptions;

            GetProjectKey();
        }

        private void GetProjectKey()
        {
            foreach (var item in analyzerOptions.AdditionalFiles)
            {
                var match = SonarLintFileRegEx.Match(item.Path);
                if (match.Success)
                {
                    SonarProjectKey = match.Groups["sonarkey"].Value;
                    return;
                }
            }
        }

        public bool IsInConnectedMode => SonarProjectKey != null;

        public string SonarProjectKey { get; private set; } = null;
    }
}
