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

using System.IO;

namespace SonarLint.VisualStudio.Integration.NodeJS.Locator.LocationProviders
{
    internal class GlobalPathNodeLocationsProvider : INodeLocationsProvider
    {
        private const string FileName = "node.exe";

        /// <summary>
        /// Based on MS nodejstools repo: https://github.com/microsoft/nodejstools/blob/275e85d5cd95cad9122f59a76f9e49bead66101b/Nodejs/Product/Nodejs/Nodejs.cs#L112
        /// </summary>
        public IReadOnlyCollection<string> Get()
        {
            var nodeCandidatePaths = new List<string>();
            var pathVar = Environment.GetEnvironmentVariable("PATH");

            if (pathVar != null)
            {
                // If we didn't find node.js in the registry we should look at the user's path.
                foreach (var dir in pathVar.Split(Path.PathSeparator))
                {
                    try
                    {
                        var execPath = Path.Combine(dir, FileName);
                        nodeCandidatePaths.Add(execPath);
                    }
                    catch (ArgumentException)
                    {
                        /*noop*/
                    }
                }
            }

            var programFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", FileName);
            nodeCandidatePaths.Add(programFilesPath);

            var x86ProgramFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            if (!string.IsNullOrEmpty(x86ProgramFilesFolder))
            {
                programFilesPath = Path.Combine(x86ProgramFilesFolder, "nodejs", FileName);
                nodeCandidatePaths.Add(programFilesPath);
            }

            return nodeCandidatePaths;
        }
    }
}
