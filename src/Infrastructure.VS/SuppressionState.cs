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

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    // This class duplicates SuppressionState enum and StandardTableKeyNames.SuppressionState because it's not available in vs2019 assembly (even though it is available in the ide)
    internal static class SuppressionState
    {
        public const string ColumnName = "suppression";

        public const int ActiveEnumValue = 0;
        public const int SuppressedEnumValue = 1;
        public const int NotApplicableEnumValue = 2;
    }
}
