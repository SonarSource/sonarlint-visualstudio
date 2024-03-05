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

public static class PluginInformationLoader // this might be reused in the product code in the future
{
    public static List<string> EnsurePluginsAreAvailable()
    {
        var analyzerProps = new XmlDocument();
        analyzerProps.Load("EmbeddedSonarAnalyzer.props");

        var roslynAnalyzerVersion = GetAnalyzerVersion("EmbeddedSonarAnalyzerVersion", analyzerProps);
        var cfamilyAnalyzerVersion = GetAnalyzerVersion("EmbeddedSonarCFamilyAnalyzerVersion", analyzerProps);
        var jstsAnalyzerVersion = GetAnalyzerVersion("EmbeddedSonarJSAnalyzerVersion", analyzerProps);
        var secrestsAnalyzerVersion = GetAnalyzerVersion("EmbeddedSonarSecretsJarVersion", analyzerProps);

        var jarDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLVS_Build_DownloadedJars");

        var jars = Directory.GetFiles(jarDirectoryPath);

        return new List<string>
        {
            GetAnalyzerPath("sonar-csharp-plugin", roslynAnalyzerVersion, jars),
            GetAnalyzerPath("sonar-vbnet-plugin", roslynAnalyzerVersion, jars),    
            GetAnalyzerPath("sonar-cfamily-plugin", cfamilyAnalyzerVersion, jars),
            GetAnalyzerPath("sonar-javascript-plugin", jstsAnalyzerVersion, jars),
            GetAnalyzerPath("sonar-text-plugin", secrestsAnalyzerVersion, jars),
        };
    }

    private static string GetAnalyzerPath(string analyzerFileName, string analyzerVersion, string[] analyzerJars) => 
        analyzerJars.First(x => Path.GetFileName(x) == $"{analyzerFileName}-{analyzerVersion}.jar");

    private static string GetAnalyzerVersion(string tagName, XmlDocument xmlDocument)
    {
        var elementsByTagName = xmlDocument.GetElementsByTagName(tagName);
        if (elementsByTagName.Count != 1)
        {
            throw new InvalidOperationException();
        }

        return elementsByTagName[0].InnerText;
    }
}
