/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix.SLCore;

internal interface IGlobalJreProvider
{
    string JreFullPath { get; }
}

[Export(typeof(IGlobalJreProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[ExcludeFromCodeCoverage]
internal class GlobalJreProvider : IGlobalJreProvider
{
    private readonly Lazy<string> pathOrNull;
    private readonly ILogger log;

    [method: ImportingConstructor]
    public GlobalJreProvider(ILogger log)
    {
        this.log = log.ForVerboseContext(nameof(GlobalJreProvider));
        pathOrNull = new(() => GetFromJavaHome() ?? GetFromWhereCommand(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string JreFullPath => pathOrNull.Value;

    private string GetFromJavaHome()
    {
        const string javaHome = "JAVA_HOME";

        try
        {
            var javaHomeValue = Environment.GetEnvironmentVariable(javaHome);
            if (!string.IsNullOrEmpty(javaHomeValue))
            {
                var javaExecutablePath = Path.Combine(javaHomeValue, JavaLocationConstants.WindowsJreSubPath);
                if (File.Exists(javaExecutablePath))
                {
                    log.WriteLine(Strings.GlobalJreProvider_JavaExecutableFound, javaHome, javaExecutablePath);
                    return javaExecutablePath;
                }
            }
            log.WriteLine(Strings.GlobalJreProvider_CouldNotLocateJava, javaHome);
        }
        catch (Exception ex)
        {
            log.WriteLine(Strings.GlobalJreProvider_ErrorFindingJava, javaHome, ex);
        }
        return null;
    }

    private string GetFromWhereCommand()
    {
        const string whereExe = "C:\\Windows\\System32\\where.exe";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = whereExe,
                Arguments = "java.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            var javaExecutablePathOrNull = output
                .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (File.Exists(javaExecutablePathOrNull))
            {
                log.WriteLine(Strings.GlobalJreProvider_JavaExecutableFound, whereExe, javaExecutablePathOrNull);
                return javaExecutablePathOrNull;
            }

            log.WriteLine(Strings.GlobalJreProvider_CouldNotLocateJava, whereExe);
        }
        catch (Exception ex)
        {
            log.WriteLine(Strings.GlobalJreProvider_ErrorFindingJava, whereExe, ex);
        }

        return null;
    }
}
