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

namespace SonarLint.VisualStudio.IssueVisualization.Commands
{
    internal static class Constants
    {
        public static readonly Guid CommandSetGuid = new Guid("FDEF405A-28C2-4AFD-A37B-49EF2B0D142E");
        public const string UIContextGuid = "f83e901e-41cb-4faf-8116-aacb1b385381";

        // EnvDTE.Constants.vsWindowKindProperties
        public const string DefaultDockWindowGuid = "EEFA5220-E298-11D0-8F78-00A0C9110057";
    }
}
