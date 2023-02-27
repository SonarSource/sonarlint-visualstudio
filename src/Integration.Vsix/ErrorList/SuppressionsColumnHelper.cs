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

using Microsoft.VisualStudio.Shell.TableManager;

namespace SonarLint.VisualStudio.Integration.Vsix.ErrorList
{
    /// <summary>
    /// Handles the differences between VS2019 and VS2022 in using the Suppression State column
    /// in the Error List.
    /// </summary>
    /// <remarks>Some of the suppressions-related constants/types are either not publicly available in VS2019.3
    /// See https://github.com/SonarSource/sonarlint-visualstudio/issues/3797
    /// </remarks>
    internal static class SuppressionsColumnHelper
    {
#if VS2022
        // The VS 2022 SDK exposes the constants/types we need so we just reference them directly.

        public static readonly object SuppressionState_Active = Boxes.SuppressionState.Active;
        public static readonly object SuppressionState_Suppressed = Boxes.SuppressionState.Suppressed;
        
        public const string SuppressionStateColumnName = StandardTableKeyNames.SuppressionState;

#else
        // String alternatives for values in enum Microsoft.VisualStudio.Shell.TableManager.SuppressionState.
        // The enum isn't available in SDKs that are compatible with VS2019.3.
        // Fortunately, VS will parse the string to the enum for us.
        public static readonly object SuppressionState_Active = "Active";
        public static readonly object SuppressionState_Suppressed = "Suppressed";
        
        /// <summary>
        /// Redefinition of <see cref="StandardTableKeyNames.SuppressionState"></see>.
        /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.tablemanager.standardtablekeynames?view=visualstudiosdk-2022
        /// </summary>
        public const string SuppressionStateColumnName = "suppression";

#endif
    }
}
