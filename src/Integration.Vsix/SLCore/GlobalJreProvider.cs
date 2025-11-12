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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SonarLint.VisualStudio.Core;

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
        pathOrNull = new(GetJavaExecutablePathOrNull, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string JreFullPath => pathOrNull.Value;

    private string GetJavaExecutablePathOrNull()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
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
            return output
                .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            log.WriteLine($"Error finding java.exe: {ex.Message}");
            return null;
        }
    }
}
