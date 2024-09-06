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
using System.Linq;
using System.Xml;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

public class DependencyLocator // this might be reused in the product code in the future
{
    private static readonly XmlDocument dependencyProps;
    private static readonly string localAppData;
    public static List<string> AnalyzerPlugins { get; private set; }
    public static string SloopBasePath { get; private set; }

    static DependencyLocator()
    {
        localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        dependencyProps = new XmlDocument();
        dependencyProps.Load("EmbeddedSonarAnalyzer.props");
        
        EnsureSloopIsAvailable(localAppData, dependencyProps);
        EnsurePluginsAreAvailable();
    }

    internal static void EnsurePluginsAreAvailable()
    {
        var jarDirectoryPath = Path.Combine(localAppData, "SLVS_Build_DownloadedJars");
        var availablePluginJars = Directory.GetFiles(jarDirectoryPath);
        var roslynAnalyzerVersion = GetDependencyVersion("EmbeddedSonarAnalyzerVersion", dependencyProps);
        var cfamilyAnalyzerVersion = GetDependencyVersion("EmbeddedSonarCFamilyAnalyzerVersion", dependencyProps);
        var jstsAnalyzerVersion = GetDependencyVersion("EmbeddedSonarJSAnalyzerVersion", dependencyProps);
        var secrestsAnalyzerVersion = GetDependencyVersion("EmbeddedSonarSecretsJarVersion", dependencyProps);
        AnalyzerPlugins = new List<string>
        {
            GetAnalyzerPath("sonar-csharp-plugin", roslynAnalyzerVersion, availablePluginJars),
            GetAnalyzerPath("sonar-vbnet-plugin", roslynAnalyzerVersion, availablePluginJars),    
            GetAnalyzerPath("sonar-cfamily-plugin", cfamilyAnalyzerVersion, availablePluginJars),
            GetAnalyzerPath("sonar-javascript-plugin", jstsAnalyzerVersion, availablePluginJars),
            GetAnalyzerPath("sonar-text-plugin", secrestsAnalyzerVersion, availablePluginJars),
        };
    }

    private static void EnsureSloopIsAvailable(string localAppData, XmlDocument dependencyProps)
    {
        var sloopVersion = GetDependencyVersion("EmbeddedSloopVersion", dependencyProps);
        var sloopPath = Path.Combine(localAppData,
            "SLVS_Build_Sloop",
            $"sonarlint-backend-cli-{sloopVersion}-windows_x64");
        if (!Directory.Exists(sloopPath))
        {
            throw new InvalidOperationException($"Can't locate SLOOP {sloopVersion}");
        }

        SloopBasePath = sloopPath;
    }

    private static string GetAnalyzerPath(string analyzerFileName, string analyzerVersion, string[] analyzerJars)
    {
        var analyzerPath = analyzerJars.FirstOrDefault(x => Path.GetFileName(x) == $"{analyzerFileName}-{analyzerVersion}.jar");
        if (analyzerPath == default)
        {
            throw new InvalidOperationException($"Can't locate {analyzerFileName} {analyzerVersion}");
        }
        return analyzerPath;
    }

    private static string GetDependencyVersion(string tagName, XmlDocument dependencyProps)
    {
        var elementsByTagName = dependencyProps.GetElementsByTagName(tagName);
        if (elementsByTagName.Count != 1)
        {
            throw new InvalidOperationException($"No version found for {tagName}");
        }

        return elementsByTagName[0].InnerText;
    }
}
