/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface IMacroEvaluationService
    {
        /// <summary>
        /// Evaluates any macros and environment variables in the input path and returns the 
        /// </summary>
        /// <param name="input">The string to be evaluated</param>
        /// <param name="activeConfiguration">The current build configuration</param>
        /// <param name="workspaceRootDir">The workspace root directory</param>
        /// <returns>The evaluated result, or null if the input contained properties that could not be evaluated.</returns>
        string Evaluate(string input, string activeConfiguration, string workspaceRootDir);
    }

    internal class MacroEvaluationService : IMacroEvaluationService
    {
        public string Evaluate(string input, string activeConfiguration, string workspaceRootDir)
        {
            if (input == null)
            {
                return null;
            }
            
            var output = input
                .Replace("${projectDir}", workspaceRootDir)
                .Replace("${name}", activeConfiguration);
            return output;
        }
    }
}
