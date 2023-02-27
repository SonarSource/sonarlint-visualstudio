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

namespace SonarLint.VisualStudio.Integration.Vsix.ErrorList
{
    /// <summary>
    /// Redefinitions of VS constants/types that are not available in VS2019 update 3 (i.e. v16.3)
    /// </summary>
    /// <remarks>Some of the suppressions-related constants are either not publicly available in VS2019.3
    /// See https://github.com/SonarSource/sonarlint-visualstudio/issues/3797
    /// </remarks>
    internal static class RedefinedErrorListConstants
    {
        /// <summary>
        /// Redefinition of <see cref="StandardTableKeyNames.SuppressionState"></see>.
        /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.tablemanager.standardtablekeynames?view=visualstudiosdk-2022
        /// </summary>
        /// The constant is not available in VS v16.3 (?becomes available in v16.4?), so we're
        /// defining our own version of it here. It's just a string, so there are no type-equivalent
        /// issues to worry about.
        public const string SuppressionStateColumnName = "suppression";

        // String alternatives for values in enum Microsoft.VisualStudio.Shell.TableManager.SuppressionState.
        public const string SuppressionState_Active = "Active";
        public const string SuppressionState_Suppressed = "Suppressed";
    }
}
