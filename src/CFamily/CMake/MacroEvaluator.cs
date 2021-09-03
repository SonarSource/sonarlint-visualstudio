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
    internal class EvaluationContext
    {
        public EvaluationContext(string activeConfiguration, string rootDirectory)
        {
            ActiveConfiguration = activeConfiguration;
            RootDirectory = rootDirectory;
        }

        public string RootDirectory { get; }

        public string ActiveConfiguration { get; }
    }

    /// <summary>
    /// Returns the evaluated value for the supplied macro, or null if it could not be evaluated
    /// </summary>
    internal interface IMacroEvaluator
    {
        string TryEvaluate(string macroPrefix, string macroName, EvaluationContext context);
    }

    internal class MacroEvaluator : IMacroEvaluator
    {
        public string TryEvaluate(string macroPrefix, string macroName, EvaluationContext evaluationContext)
        {
            // TODO:
            // * process other simple macros
            // * process environment variable macros
            if (macroPrefix != string.Empty)
            {
                return null;
            }

            if (macroName == "projectDir")
            {
                return evaluationContext.RootDirectory;
            }

            if (macroName == "name")
            {
                return evaluationContext.ActiveConfiguration;
            }

            return null;
        }
    }
}
