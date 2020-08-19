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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    // Properties are settable to simplify creating test instances
    public class DummyAnalysisIssue : IAnalysisIssue
    {
        public string RuleKey { get; set; }

        public AnalysisIssueSeverity Severity { get; set; }

        public AnalysisIssueType Type { get; set; }

        public int StartLine { get; set; }

        public int EndLine { get; set; }

        public int StartLineOffset { get; set; }

        public int EndLineOffset { get; set; }

        public string LineHash { get; set; }

        public string Message { get; set; }

        public string FilePath { get; set; }

        public IReadOnlyList<IAnalysisIssueFlow> Flows { get; } = Array.Empty<IAnalysisIssueFlow>();
    }
}
