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
using System.IO;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    /// <summary>
    /// Returns the evaluated value for the supplied macro, or null if it could not be evaluated
    /// </summary>
    internal interface IMacroEvaluator
    {
        string TryEvaluate(string macroPrefix, string macroName, IEvaluationContext context);
    }

    internal class MacroEvaluator : IMacroEvaluator
    {
        private readonly IEnvironmentVariableProvider environmentVariableProvider;

        public MacroEvaluator()
            : this(EnvironmentVariableProvider.Instance)
        {
        }

        internal MacroEvaluator(IEnvironmentVariableProvider environmentVariableProvider)
        {
            this.environmentVariableProvider = environmentVariableProvider;
        }

        public string TryEvaluate(string macroPrefix, string macroName, IEvaluationContext context)
        {
            if (string.IsNullOrEmpty(macroName))
            {
                return null;
            }

            if (macroPrefix != string.Empty)
            {
                return macroPrefix.Equals("env", StringComparison.CurrentCultureIgnoreCase)
                    ? environmentVariableProvider.TryGet(macroName)
                    : null;
            }

            switch (macroName)
            {
                case "workspaceRoot":
                    return context.RootDirectory;
                case "workspaceHash": // workspaceHash and projectHash both use the root CMakeLists file path
                case "projectHash":
                    return CMakeHashCalculator.CalculateVS2019Guid(context.RootCMakeListsFilePath).ToString();
                case "projectFile":
                    return context.RootCMakeListsFilePath;
                case "projectDir":
                    return Path.GetDirectoryName(context.RootCMakeListsFilePath);
                case "projectDirName":
                {
                    var directoryName = Path.GetDirectoryName(context.RootCMakeListsFilePath);
                    return string.IsNullOrEmpty(directoryName) ? null : new DirectoryInfo(directoryName).Name;
                }
                case "thisFile":
                    return context.CMakeSettingsFilePath;
                case "thisFileDir":
                    return Path.GetDirectoryName(context.CMakeSettingsFilePath);
                case "name":
                    return context.ActiveConfiguration;
                case "generator":
                    return context.Generator;
                default:
                    return null;
            }
        }
    }
}
