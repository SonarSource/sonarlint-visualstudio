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
using System.Linq;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal interface IPchCacheCleaner
    {
        void Cleanup();
    }

    internal class PchCacheCleaner : IPchCacheCleaner
    {
        private readonly IFileSystem fileSystem;
        private readonly string pchFilePath;

        public PchCacheCleaner(IFileSystem fileSystem, string pchFilePath)
        {
            this.fileSystem = fileSystem;
            this.pchFilePath = pchFilePath;
        }

        public void Cleanup() => DeletePchFiles();
        
        private void DeletePchFiles()
        {
            var pchDirectory = Path.GetDirectoryName(pchFilePath);
            var pchFileName = Path.GetFileName(pchFilePath);

            var filesToDelete = fileSystem.Directory.GetFiles(pchDirectory, $"{pchFileName}.*").ToList();
            filesToDelete.Add(pchFilePath);

            foreach (var fileToDelete in filesToDelete)
            {
                try
                {
                    fileSystem.File.Delete(fileToDelete);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    // nothing to do if we fail to delete
                }
            }
        }
    }
}
