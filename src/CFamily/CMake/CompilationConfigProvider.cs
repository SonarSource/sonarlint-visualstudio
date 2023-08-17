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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Helpers;

using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface ICompilationConfigProvider
    {
        /// <summary>
        /// Returns the compilation configuration for the given file,
        /// as specified in the compilation database file for the currently active build configuration.
        /// Returns null if there is no compilation database or if the file does not exist in the compilation database.
        /// </summary>
        CompilationDatabaseEntry GetConfig(string filePath);
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

        public CompilationDatabaseEntry GetConfig(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var compilationDatabaseLocation = compilationDatabaseLocator.Locate();

            if (string.IsNullOrEmpty(compilationDatabaseLocation))
            {
                return null;
            }

            logger.LogVerbose($"[CompilationConfigProvider] Reading compilation database from '{compilationDatabaseLocation}'");

            try
            {
                var compilationDatabaseString = fileSystem.File.ReadAllText(compilationDatabaseLocation);
                var compilationDatabaseEntries = JsonConvert.DeserializeObject<IEnumerable<CompilationDatabaseEntry>>(compilationDatabaseString);

                if (compilationDatabaseEntries == null || !compilationDatabaseEntries.Any())
                {
                    logger.WriteLine(Resources.EmptyCompilationDatabaseFile, compilationDatabaseLocation);
                    return null;
                }

                var stopwatch = Stopwatch.StartNew();

                var entry = CFamilyShared.IsHeaderFileExtension(filePath)
                    ? LocateMatchingCodeEntry(filePath, compilationDatabaseEntries)
                    : LocateExactCodeEntry(filePath, compilationDatabaseEntries);

                // todo: remove before release
                logger.LogVerbose("***** [CompilationConfigProvider] time (ms) to locate entry: " + stopwatch.ElapsedMilliseconds);

                return entry;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.BadCompilationDatabaseFile, ex);

                return null;
            }
        }

        private CompilationDatabaseEntry LocateExactCodeEntry(string filePath, IEnumerable<CompilationDatabaseEntry> compilationDatabaseEntries)
        {
            logger.LogVerbose($"[CompilationConfigProvider] Code file detected, searching for exact match. File: {filePath}");

            var entry = LocateCodeEntry(filePath, compilationDatabaseEntries);

            if (entry == null)
            {
                logger.WriteLine(Resources.NoCompilationDatabaseEntry, filePath);
            }

            return entry;
        }

        private CompilationDatabaseEntry LocateMatchingCodeEntry(string headerFilePath, IEnumerable<CompilationDatabaseEntry> compilationDatabaseEntries)
        {
            logger.LogVerbose($"[CompilationConfigProvider] Header file detected, searching for matching code file. File: {headerFilePath}");

            var codeFilesWithSameNameAndSamePath =
                CFamilyShared.KnownExtensions.Select(ext => Path.ChangeExtension(headerFilePath, ext));

            var matchingCodeEntry =
                LocateCodeEntryWithExactNameAndPath(codeFilesWithSameNameAndSamePath, compilationDatabaseEntries) ??
                LocateCodeEntryWithExactName(codeFilesWithSameNameAndSamePath, compilationDatabaseEntries) ??
                LocateFirstCodeEntryUnderRoot(headerFilePath, compilationDatabaseEntries) ??
                LocateFirstCodeEntry(compilationDatabaseEntries);

            if (matchingCodeEntry == null)
            {
                logger.WriteLine(Resources.NoCompilationDatabaseEntryForHeaderFile, headerFilePath);
            }

            return matchingCodeEntry;
        }

        private CompilationDatabaseEntry LocateCodeEntryWithExactNameAndPath(IEnumerable<string> codeFilesWithSameNameAndSamePath, IEnumerable<CompilationDatabaseEntry> compilationDatabaseEntries)
        {
            foreach (var codeFile in codeFilesWithSameNameAndSamePath)
            {
                var entry = LocateCodeEntry(codeFile, compilationDatabaseEntries);

                if (entry != null)
                {
                    logger.LogVerbose($"[CompilationConfigProvider] Header file: located matching code file with same name and path: {entry.File}");
                    return entry;
                }
            }

            return null;
        }

        private CompilationDatabaseEntry LocateCodeEntryWithExactName(IEnumerable<string> codeFilesWithSameNameAndSamePath, IEnumerable<CompilationDatabaseEntry> compilationDatabaseEntries)
        {
            var codeFilesWithSameName = codeFilesWithSameNameAndSamePath.Select(Path.GetFileName);

            foreach (var codeFile in codeFilesWithSameName)
            {
                var entry = compilationDatabaseEntries.FirstOrDefault(x =>
                    !string.IsNullOrEmpty(x.File) &&
                    Path.GetFileName(x.File).Equals(codeFile, StringComparison.OrdinalIgnoreCase));

                if (entry != null)
                {
                    logger.LogVerbose($"[CompilationConfigProvider] Header file: located matching code file with same name: {entry.File}");
                    return entry;
                }
            }

            return null;
        }

        private CompilationDatabaseEntry LocateFirstCodeEntryUnderRoot(string headerFilePath, IEnumerable<CompilationDatabaseEntry> compilationDatabaseEntries)
        {
            var rootDirectory = Path.GetDirectoryName(headerFilePath);

            var entry = compilationDatabaseEntries.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.File) &&
                PathHelper.IsPathRootedUnderRoot(x.File, rootDirectory));

            if (entry != null)
            {
                logger.LogVerbose($"[CompilationConfigProvider] Header file: located code file under same root: {entry.File}");
            }

            return entry;
        }

        private CompilationDatabaseEntry LocateFirstCodeEntry(IEnumerable<CompilationDatabaseEntry> compilationDatabaseEntries)
        {
            var entry = compilationDatabaseEntries.FirstOrDefault(x => !string.IsNullOrEmpty(x.File));

            if (entry != null)
            {
                logger.LogVerbose($"[CompilationConfigProvider] Header file: using first entry: {entry.File}");
            }

            return entry;
        }

        private static CompilationDatabaseEntry LocateCodeEntry(string filePath, IEnumerable<CompilationDatabaseEntry> compilationDatabaseEntries) =>
            compilationDatabaseEntries.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.File) &&
                PathHelper.IsMatchingPath(filePath, x.File));
    }
}
