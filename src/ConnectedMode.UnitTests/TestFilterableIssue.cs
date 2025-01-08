﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.Suppressions;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

internal class TestFilterableIssue : IFilterableIssue
{
    public Guid? IssueId { get; set; }
    public string RuleId { get; set; }
    public string LineHash { get; set; }
    public int? StartLine { get; set; }
    public string FilePath { get; set; }

    // Not expecting the other property to be used
    public string WholeLineText => throw new NotImplementedException();
}
