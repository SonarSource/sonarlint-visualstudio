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

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

public static class IssueListIds
{
    public const string TaintIdAsString = "537833A5-E0F1-4405-821D-D83D89370B78";
    public static readonly Guid TaintId = new Guid(TaintIdAsString);
    public const string HotspotsIdAsString = "D71842F7-4DB3-4AC1-A91A-D16D1A514242";
    public static readonly Guid HotspotsId = new Guid(HotspotsIdAsString);
    public static readonly Guid ErrorListId = new Guid(ToolWindowGuids80.ErrorList);
}
