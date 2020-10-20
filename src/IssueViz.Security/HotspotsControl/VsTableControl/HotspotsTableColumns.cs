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

extern alias versionSpecificShellFramework;

using System.Collections.Generic;
using System.Linq;

using versionSpecificShellFramework::Microsoft.VisualStudio.Shell.TableControl;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl.VsTableControl
{
    internal static class HotspotsTableColumns
    {
        public static IReadOnlyList<ColumnState2> InitialStates { get; } = new[]
        {
            new ColumnState2(StandardTableColumnDefinitions.DocumentName, true, 200),
            new ColumnState2(StandardTableColumnDefinitions.ProjectName, true, 200),
            new ColumnState2(StandardTableColumnDefinitions.Line, true, 200),
            new ColumnState2(StandardTableColumnDefinitions.Column, true, 200),
        };

        public static string[] Names { get; } = InitialStates.Select(x => x.Name).ToArray();
    }
}
