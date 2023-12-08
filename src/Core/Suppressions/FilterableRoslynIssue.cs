/*
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

namespace SonarLint.VisualStudio.Core.Suppressions
{
    /// <summary>
    /// Represents the subset of roslyn issue properties used for issue muting. Supports delayed hash calculation.
    /// </summary>
    public interface IFilterableRoslynIssue : IFilterableIssue
    {
        void SetLineHash(string lineHash);
        int RoslynStartLine { get; }
        int RoslynStartColumn { get; }
    }

    public class FilterableRoslynIssue : IFilterableRoslynIssue
    {
        public FilterableRoslynIssue(string ruleId, string filePath, int startLine, int startColumn)
        {
            RuleId = ruleId;
            FilePath = filePath;
            RoslynStartLine = startLine;
            RoslynStartColumn = startColumn;
        }

        public string RuleId { get; }
        public string FilePath { get; }
        public int? StartLine => RoslynStartLine;
        public int RoslynStartLine { get; }
        public int RoslynStartColumn { get; }
        public string LineHash { get; private set; }
        
        public void SetLineHash(string lineHash)
        {
            LineHash = lineHash;
        }
    }
}
