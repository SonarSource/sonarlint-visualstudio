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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.Integration.Vsix.SLCore;

[Export(typeof(ISLCoreEmbeddedPluginJarLocator))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SLCoreEmbeddedPluginJarLocator : ISLCoreEmbeddedPluginJarLocator
{
    private const string JarFolderName = "DownloadedJars";
    private readonly IVsixRootLocator vsixRootLocator;
    private readonly IFileSystem fileSystem;

    [ImportingConstructor]
    public SLCoreEmbeddedPluginJarLocator(IVsixRootLocator vsixRootLocator) : this(vsixRootLocator, new FileSystem()) { }

    internal SLCoreEmbeddedPluginJarLocator(IVsixRootLocator vsixRootLocator, IFileSystem fileSystem)
    {
        this.vsixRootLocator = vsixRootLocator;
        this.fileSystem = fileSystem;
    }

    public List<string> ListJarFiles()
    {
        var jarFolderPath = Path.Combine(vsixRootLocator.GetVsixRoot(), JarFolderName);

        if (fileSystem.Directory.Exists(jarFolderPath))
        {
            return fileSystem.Directory.GetFiles(jarFolderPath, "*.jar").ToList();
        }
        return new List<string>();
    }
}
