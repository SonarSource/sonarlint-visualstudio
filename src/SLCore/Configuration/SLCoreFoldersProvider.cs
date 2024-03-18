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
using System.ComponentModel.Composition;
using System.IO;

namespace SonarLint.VisualStudio.SLCore.Configuration
{
    [Export(typeof(ISLCoreFoldersProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SLCoreFoldersProvider : ISLCoreFoldersProvider
    {
        private const string storageRoot = "storageRoot";
        private const string workDir = "workDir";

        public SLCoreFolders GetWorkFolders()
        {
            //Other ide's pass userHome as null so we comply this behaviour
            return new SLCoreFolders(CombinePath(storageRoot), CombinePath(workDir), null);
        }

        private string CombinePath(string path)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLVS_SLOOP", path);
        }
    }
}
