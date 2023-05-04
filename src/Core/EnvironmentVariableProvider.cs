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

using System;
using System.Diagnostics;
using System.IO;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Wrapper around <see cref="System.Environment"/> for testing
    /// </summary>
    public interface IEnvironmentVariableProvider
    {
        /// <summary>
        /// Returns the value of the given variable, or null if the variable does not exist.
        /// </summary>
        string TryGet(string variableName);

        /// <summary>
        /// Gets the path to the system special folder that is identified by the specified enumeration.
        /// </summary>
        string GetFolderPath(Environment.SpecialFolder folder);
    }

    public class EnvironmentVariableProvider : IEnvironmentVariableProvider
    {
        public static EnvironmentVariableProvider Instance { get; } = new EnvironmentVariableProvider();

        private EnvironmentVariableProvider()
        {
            // no-op
        }

        public string TryGet(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentNullException(nameof(variableName));
            }

            return Environment.GetEnvironmentVariable(variableName);
        }

        public string GetFolderPath(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);
    }

    /// <summary>
    /// Extension methods to return common SLVS-specific values based on environment variables
    /// </summary>
    public static class EnvironmentVariableProviderExtensions
    {
        /// <summary>
        /// Returns the root path under {User}\{AppData} to use for SLVS-specific settings
        /// </summary>
        public static string GetSLVSAppDataRootPath(this IEnvironmentVariableProvider provider)
        {
            var root = provider.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Debug.Assert(root != null, "Environment.SpecialFolder.ApplicationData should not return null");

            return Path.Combine(root, "SonarLint for Visual Studio");
        }
    }
}
