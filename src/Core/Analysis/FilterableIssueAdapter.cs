﻿/*
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
using SonarLint.VisualStudio.Core.Suppression;

namespace SonarLint.VisualStudio.Core.Analysis
{
    public class FilterableIssueAdapter : IFilterableIssue
    {
        private readonly string wholeLineText;
        private readonly string lineHash;

        public FilterableIssueAdapter(IAnalysisIssue sonarLintIssue, string wholeLineText, string lineHash)
        {
            SonarLintIssue = sonarLintIssue ?? throw new ArgumentNullException(nameof(sonarLintIssue));

            this.wholeLineText = wholeLineText;
            this.lineHash = lineHash;
        }

        public IAnalysisIssue SonarLintIssue { get; }

        string IFilterableIssue.RuleId => SonarLintIssue.RuleKey;

        string IFilterableIssue.FilePath => SonarLintIssue.FilePath;

        string IFilterableIssue.ProjectGuid { get; }

        int? IFilterableIssue.StartLine => SonarLintIssue.StartLine;

        string IFilterableIssue.WholeLineText => wholeLineText;

        string IFilterableIssue.LineHash => lineHash;
    }
}


