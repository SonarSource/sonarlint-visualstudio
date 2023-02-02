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

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Attempts to retrieve the Sonar rule key from the selected item in the Error List.
    /// </summary>
    /// <remarks>
    /// The method will only return a rule key if:
    /// * there is a single row selected in the Error List
    /// * the row represents a Sonar analysis issue for any supported language
    ///   (including Roslyn languages i.e. C# and VB.NET)
    /// </remarks>
    public interface IErrorListHelper
    {
        bool TryGetRuleIdFromSelectedRow(out SonarCompositeRuleId ruleId);
    }
}
