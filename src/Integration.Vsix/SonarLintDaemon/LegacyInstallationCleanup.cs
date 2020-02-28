/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class LegacyInstallationCleanup
    {
        // See issue https://github.com/SonarSource/sonarlint-visualstudio/issues/470
        // Previously the daemon and JVM binaries were downloaded the user's roaming profile
        // e.g. c:\users\joebloggs\appdata\roaming\sonarlint-daemon-2.14.0.669-windows
        // It's now downloaded elsewhere. This class attempts to clean up folders installed
        // to the old location.

        private readonly ILogger logger;
        private readonly string legacyStoragePath;
        private readonly IFileSystem fileSystem;

        public static void CleanupDaemonFiles(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            new LegacyInstallationCleanup(logger,
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                new FileSystem()).Clean();
        }

        internal /* for testing */ LegacyInstallationCleanup(ILogger logger, string legacyStoragePath, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.legacyStoragePath = legacyStoragePath;
            this.fileSystem = fileSystem;
        }

        public void Clean()
        {
            var foldersToDelete = fileSystem.Directory.GetDirectories(legacyStoragePath, "sonarlint-daemon-*-windows");

            if (foldersToDelete.Length == 0)
            {
                return;
            }

            logger.WriteLine($"Attempting to delete daemon binaries from legacy location: {legacyStoragePath}");

            foreach (string folder in foldersToDelete)
            {
                SafeDeleteDirectory(folder);
            }
        }

        private void SafeDeleteDirectory(string fullPath)
        {
            if (!fileSystem.Directory.Exists(fullPath))
            {
                return;
            }

            // If an instance of the daemon is running then some of the files might be locked.
            // To check this, we'll first try to rename the folder.
            // If that succeeds then we should be able to delete the folder.
            string renamedPath = fullPath + Guid.NewGuid().ToString();

            try
            {
                fileSystem.Directory.Move(fullPath, renamedPath);
                fileSystem.Directory.Delete(renamedPath, true);
                logger.WriteLine($"Deleted daemon folder {fullPath}");
            }
            catch (IOException ex)
            {
                logger.WriteLine($"Failed to delete daemon folder {fullPath}. Error: {ex.Message}");
            }
        }
    }
}
