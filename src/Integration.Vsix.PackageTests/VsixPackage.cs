/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using System.IO.Compression;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix.PackageTests;

internal sealed class PackagedAssembly(string pathInVsix, AssemblyName name)
{
    public string PathInVsix { get; } = pathInVsix;
    public AssemblyName Name { get; } = name;
}

internal sealed class VsixPackage : IDisposable
{
    private static readonly string[] BundledContentFolders = ["DownloadedJars", "Sloop", "EmbeddedDotnetAnalyzerDLLs", "EmbeddedEsLintBridge"];
    private static readonly string[] BinaryExtensions = [".dll", ".exe"];

    private readonly string extractionDirectory;

    internal VsixPackage(string extractionDirectory, IReadOnlyList<PackagedAssembly> assemblies)
    {
        this.extractionDirectory = extractionDirectory;
        Assemblies = assemblies;
    }

    public IReadOnlyList<PackagedAssembly> Assemblies { get; }

    public bool AreSonarLintAssembliesStrongNamed =>
        Assemblies.Any(x => x.Name.Name.StartsWith("SonarLint.", StringComparison.Ordinal) && x.Name.IsStrongNamed());

    public static VsixPackage Open(string vsixPath)
    {
        if (!File.Exists(vsixPath))
        {
            throw new FileNotFoundException($"VSIX not found at '{vsixPath}'. Build Integration.Vsix first.", vsixPath);
        }

        var extractionDirectory = Path.Combine(Path.GetTempPath(), "vsix-package-tests-" + Guid.NewGuid().ToString("N"));
        var assemblies = new List<PackagedAssembly>();
        using (var archive = ZipFile.OpenRead(vsixPath))
        {
            foreach (var entry in archive.Entries.Where(IsCheckedBinary))
            {
                var extractedPath = Path.Combine(extractionDirectory, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(extractedPath));
                entry.ExtractToFile(extractedPath);

                var name = AssemblyNameExtensions.TryGetAssemblyName(extractedPath);
                if (name != null)
                {
                    assemblies.Add(new PackagedAssembly(entry.FullName, name));
                }
            }
        }

        return new VsixPackage(extractionDirectory, assemblies);
    }

    public void Dispose()
    {
        if (Directory.Exists(extractionDirectory))
        {
            Directory.Delete(extractionDirectory, recursive: true);
        }
    }

    private static bool IsCheckedBinary(ZipArchiveEntry entry) =>
        BinaryExtensions.Contains(Path.GetExtension(entry.Name), StringComparer.OrdinalIgnoreCase)
        && !BundledContentFolders.Contains(entry.FullName.Split('/')[0], StringComparer.OrdinalIgnoreCase);
}
