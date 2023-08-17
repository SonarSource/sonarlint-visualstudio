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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Migration.FileProviders
{
    internal partial class XmlFileProvider
    {
        /// <summary>
        /// Searches the file system and returns matching files that are
        /// not in excluded folders (such a "bin", "obj")
        /// </summary>
        internal interface IFileFinder
        {
            IEnumerable<string> GetFiles(string rootFolder, SearchOption searchOption, params string[] searchPatterns);
        }

        internal class FileFinder : IFileFinder
        {
            internal static readonly string[] ExcludedDirectores = new string[]
            {
                "\\bin\\",
                "\\obj\\"
            };

            private readonly ILogger logger;
            private readonly IFileSystem fileSystem;

            public FileFinder(ILogger logger)
                : this(logger, new FileSystem())
            {
            }

            internal /* for testing */ FileFinder(ILogger logger, IFileSystem fileSystem)
            {
                this.logger = logger;
                this.fileSystem = fileSystem;
            }

            public IEnumerable<string> GetFiles(string rootFolder, SearchOption searchOption, params string[] searchPatterns)
            {
                logger.LogMigrationVerbose("Searching for files... Root directory: " + rootFolder);

                var timer = Stopwatch.StartNew();

                var allMatches = new List<string>();

                foreach (var pattern in searchPatterns)
                {
                    var files = fileSystem.Directory.GetFiles(rootFolder, pattern, searchOption);
                    var filesToInclude = files.Where(x => !IsInExcludedDirectory(x)).ToArray();

                    allMatches.AddRange(filesToInclude);
                    logger.LogMigrationVerbose($"  Pattern: {pattern}, Number of matching files: {filesToInclude.Length}");
                }

                timer.Stop();
                logger.LogMigrationVerbose("Total number of matching files: " + allMatches.Count);
                logger.LogMigrationVerbose("Search time (ms): " + timer.ElapsedMilliseconds);

                return allMatches;
            }

            private static bool IsInExcludedDirectory(string fullPath) => ExcludedDirectores.Any(x => fullPath.Contains(x));
        }
    }
}
