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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.VisualStudio;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface ICompilationConfigProvider
    {
        /// <summary>
        /// Returns the compilation configuration for the given file,
        /// as specified in the compilation database file for the currently active build configuration.
        /// Returns null if there is no compilation database or if the file does not exist in the compilation database.
        /// </summary>
        CompilationDatabaseEntry GetConfig(string analyzedFile);
    }

    [Export(typeof(ICompilationConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CompilationConfigProvider : ICompilationConfigProvider
    {
        private readonly ICompilationDatabaseLocator compilationDatabaseLocator;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CompilationConfigProvider(ICompilationDatabaseLocator compilationDatabaseLocator, ILogger logger)
            : this(compilationDatabaseLocator, new FileSystem(), logger)
        {
        }

        internal CompilationConfigProvider(ICompilationDatabaseLocator compilationDatabaseLocator,
            IFileSystem fileSystem, 
            ILogger logger)
        {
            this.compilationDatabaseLocator = compilationDatabaseLocator;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public CompilationDatabaseEntry GetConfig(string analyzedFile)
        {
            if (string.IsNullOrEmpty(analyzedFile))
            {
                throw new ArgumentNullException(nameof(analyzedFile));
            }

            var compilationDatabaseLocation = compilationDatabaseLocator.Locate();

            if (string.IsNullOrEmpty(compilationDatabaseLocation))
            {
                return null;
            }

            if (!fileSystem.File.Exists(compilationDatabaseLocation))
            {
                logger.WriteLine(Resources.NoCompilationDatabaseFile, compilationDatabaseLocation);

                return null;
            }

            logger.LogDebug(Resources.FoundCompilationDatabaseFile, compilationDatabaseLocation);

            try
            {
                var compilationDatabaseString = fileSystem.File.ReadAllText(compilationDatabaseLocation);
                var compilationDatabaseEntries = JsonConvert.DeserializeObject<IEnumerable<CompilationDatabaseEntry>>(compilationDatabaseString);

                var entry = compilationDatabaseEntries?.FirstOrDefault(x => !string.IsNullOrEmpty(x.File) && PathHelper.IsMatchingPath(analyzedFile, x.File));

                if (entry == null)
                {
                    logger.WriteLine(Resources.NoCompilationDatabaseEntry, analyzedFile);
                }

                return entry;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.BadCompilationDatabaseFile, ex);

                return null;
            }
        }
    }
}
