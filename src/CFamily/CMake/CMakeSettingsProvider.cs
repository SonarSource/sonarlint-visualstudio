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

using System;
using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface ICMakeSettingsProvider
    {
        CMakeSettings TryGet(string rootDirectory);
    }

    internal class CMakeSettingsProvider : ICMakeSettingsProvider
    {
        internal const string CMakeSettingsFileName = "CMakeSettings.json";

        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public CMakeSettingsProvider(ILogger logger)
            : this(logger, new FileSystem())
        {
        }

        internal CMakeSettingsProvider(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public CMakeSettings TryGet(string rootDirectory)
        {
            var cmakeSettingsFullPath = Path.GetFullPath(Path.Combine(rootDirectory, CMakeSettingsFileName));

            if (!fileSystem.File.Exists(cmakeSettingsFullPath))
            {
                return null;
            }

            logger.LogDebug($"[CompilationDatabaseLocator] Reading {cmakeSettingsFullPath}...");

            try
            {
                var settingsString = fileSystem.File.ReadAllText(cmakeSettingsFullPath);
                return JsonConvert.DeserializeObject<CMakeSettings>(settingsString);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.BadCMakeSettings, ex.Message);
                return null;
            }
        }
    }
}
