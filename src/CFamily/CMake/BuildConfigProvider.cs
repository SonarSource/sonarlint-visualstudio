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
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;
using SonarLint.VisualStudio.Core;

using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface IBuildConfigProvider
    {
        /// <summary>
        /// Returns the name of the current configuration
        /// </summary>
        /// <param name="rootDirectory">The root directory for the solution</param>
        string GetActiveConfig(string rootDirectory);
    }

    internal class BuildConfigProvider : IBuildConfigProvider
    {
        private const string VSDefaultConfiguration = "x64-Debug";
        private const string RelativeSettingsPath = ".vs\\ProjectSettings.json";
        private const string ConfigPropertyName = "CurrentProjectSetting";

        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public BuildConfigProvider(ILogger logger) : this(logger, new FileSystem())
        {
        }

        internal /* for testing */ BuildConfigProvider(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        /// <summary>
        /// Reads the config from the internal VS CMake settings file
        /// </summary>
        /// <returns>The active config, or the default config if the setting could not be found/read</returns>
        public string GetActiveConfig(string rootDirectory)
        {
            if (rootDirectory == null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }

            var fullPath = Path.Combine(rootDirectory, RelativeSettingsPath);
            LogDebug("Internal VS CMake settings file: " + fullPath);

            if (!fileSystem.File.Exists(fullPath))
            {
                LogDebug("Settings file not found. Using default config.");
                return VSDefaultConfiguration;
            }

            var activeConfig = ExtractConfig(fullPath);
            if (activeConfig == null)
            {
                LogDebug("Active config could not be read from file. Using default config.");
                return VSDefaultConfiguration;
            }

            LogDebug("Found active config. Config: " + activeConfig);
            return activeConfig;
        }

        private string ExtractConfig(string fullPath)
        {
            try
            {
                var settings = fileSystem.File.ReadAllText(fullPath);
                if (JObject.Parse(settings).TryGetValue(ConfigPropertyName, out var token))
                {
                    return token.ToString();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                LogDebug("Error reading json file: " + ex.Message);
            }
            return null;
        }

        private void LogDebug(string message)
        {
            logger.LogVerbose("[CMake:ActiveConfigProvider] " + message);
        }
    }
}
