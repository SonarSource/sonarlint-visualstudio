/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.Integration.Vsix.SLCore;

[Export(typeof(ISLCoreEmbeddedPluginJarLocator))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SLCoreEmbeddedPluginJarLocator : ISLCoreEmbeddedPluginJarLocator
{
    private const string JarFolderName = "DownloadedJars";

    private readonly Dictionary<string, string> connectedModePluginNamePatternByPluginKey = new()
    {
        { "text", "sonar-text-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar" },
        { "javascript", "sonar-javascript-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar" }
    };
    private readonly IVsixRootLocator vsixRootLocator;
    private readonly IFileSystem fileSystem;
    private readonly ILogger logger;

    [ImportingConstructor]
    public SLCoreEmbeddedPluginJarLocator(IVsixRootLocator vsixRootLocator, ILogger logger) : this(vsixRootLocator, new FileSystem(), logger) { }

    internal SLCoreEmbeddedPluginJarLocator(IVsixRootLocator vsixRootLocator, IFileSystem fileSystem, ILogger logger)
    {
        this.vsixRootLocator = vsixRootLocator;
        this.fileSystem = fileSystem;
        this.logger = logger;
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

    public Dictionary<string, string> ListConnectedModeEmbeddedPluginPathsByKey()
    {
        var connectedModeEmbeddedPluginPathsByKey = new Dictionary<string, string>();
        var embeddedPluginFilePaths = ListJarFiles();

        foreach (var kvp in connectedModePluginNamePatternByPluginKey)
        {
            if (GetPathByPluginKey(embeddedPluginFilePaths, kvp.Key, kvp.Value) is {} pluginFilePath)
            {
                connectedModeEmbeddedPluginPathsByKey.Add(kvp.Key, pluginFilePath);
            }
        }

        return connectedModeEmbeddedPluginPathsByKey;
    }

    private string GetPathByPluginKey(List<string> pluginFilePaths, string pluginKey, string pluginNameRegexPattern)
    {
        var regex = new Regex(pluginNameRegexPattern);
        var matchedFilePaths = pluginFilePaths.Where(jar => regex.IsMatch(jar)).ToList();
        switch (matchedFilePaths.Count)
        {
            case 0:
                logger.LogVerbose(Strings.ConnectedModeEmbeddedPluginJarLocator_JarsNotFound);
                break;
            case > 1:
                logger.LogVerbose(Strings.ConnectedModeEmbeddedPluginJarLocator_MultipleJars, pluginKey);
                break;
        }
        return matchedFilePaths.FirstOrDefault();
    }
}
