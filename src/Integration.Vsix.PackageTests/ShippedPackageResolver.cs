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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix.PackageTests;

/// <summary>
/// Decides whether an assembly referenced by our code must ship in the VSIX, mirroring how VSSDK packages NuGet dependencies:
/// assemblies that no restored package provides come from the .NET Framework, and assemblies on VSSDK's SuppressFromVsix list
/// are provided by Visual Studio, unless Integration.Vsix explicitly includes their package.
/// </summary>
internal sealed class ShippedPackageResolver
{
    private const string VsSdkPackageId = "Microsoft.VSSDK.BuildTools";
    private static readonly Regex ExplicitPackageInclude = new("NuGetPackageId\\)' == '(?<id>[^']+)'", RegexOptions.None, TimeSpan.FromSeconds(1));

    private readonly Dictionary<string, string> packageByAssembly;
    private readonly HashSet<string> suppressedAssemblies;
    private readonly HashSet<string> explicitlyIncludedPackages;

    internal ShippedPackageResolver(
        Dictionary<string, string> packageByAssembly,
        HashSet<string> suppressedAssemblies,
        HashSet<string> explicitlyIncludedPackages)
    {
        this.packageByAssembly = packageByAssembly;
        this.suppressedAssemblies = suppressedAssemblies;
        this.explicitlyIncludedPackages = explicitlyIncludedPackages;
    }

    public static ShippedPackageResolver Load(string vsixProjectDirectory, string nuGetPackageRoot)
    {
        var packages = ReadLockedPackages(Path.Combine(vsixProjectDirectory, "packages.lock.json"));
        var packageDirectories = packages.ToDictionary(
            x => x.Key,
            x => GetPackageDirectory(nuGetPackageRoot, x.Key, x.Value),
            StringComparer.OrdinalIgnoreCase);

        return new ShippedPackageResolver(
            ReadPackageAssemblies(packageDirectories),
            ReadSuppressedAssemblies(Path.Combine(packageDirectories[VsSdkPackageId], "tools", "vssdk")),
            ReadExplicitlyIncludedPackages(Path.Combine(vsixProjectDirectory, "Integration.Vsix.csproj")));
    }

    /// <returns>The package the assembly must be shipped from, or null when Visual Studio or the .NET Framework provides it.</returns>
    public string FindPackageToShip(AssemblyName assembly)
    {
        if (!packageByAssembly.TryGetValue(Key(assembly), out var package))
        {
            return null;
        }
        var providedByVisualStudio = suppressedAssemblies.Contains(assembly.Name) && !explicitlyIncludedPackages.Contains(package);
        return providedByVisualStudio ? null : package;
    }

    private static Dictionary<string, string> ReadLockedPackages(string lockFilePath) =>
        JObject.Parse(File.ReadAllText(lockFilePath))["dependencies"]
            .Children<JProperty>()
            .SelectMany(framework => framework.Value.Children<JProperty>())
            .Where(package => (string)package.Value["type"] != "Project")
            .GroupBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => (string)group.First().Value["resolved"], StringComparer.OrdinalIgnoreCase);

    private static string GetPackageDirectory(string nuGetPackageRoot, string id, string version)
    {
        var directory = Path.Combine(nuGetPackageRoot, id.ToLowerInvariant(), version);
        if (!Directory.Exists(directory))
        {
            throw new InvalidOperationException($"Package {id} {version} is not restored under '{nuGetPackageRoot}'. Restore the solution first.");
        }
        return directory;
    }

    private static Dictionary<string, string> ReadPackageAssemblies(Dictionary<string, string> packageDirectories)
    {
        var packageByAssembly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packageDirectories)
        {
            var libDirectory = Path.Combine(package.Value, "lib");
            if (!Directory.Exists(libDirectory))
            {
                continue;
            }
            foreach (var dll in Directory.EnumerateFiles(libDirectory, "*.dll", SearchOption.AllDirectories))
            {
                var name = AssemblyNameExtensions.TryGetAssemblyName(dll);
                if (name != null)
                {
                    packageByAssembly[Key(name)] = package.Key;
                }
            }
        }
        return packageByAssembly;
    }

    private static HashSet<string> ReadSuppressedAssemblies(string vsSdkTargetsDirectory)
    {
        var suppressed = Directory.EnumerateFiles(vsSdkTargetsDirectory, "*.targets")
            .SelectMany(file => XDocument.Load(file).Descendants().Where(element => element.Name.LocalName == "SuppressFromVsix"))
            .Select(element => Path.GetFileNameWithoutExtension((string)element.Attribute("Include")))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (suppressed.Count == 0)
        {
            throw new InvalidOperationException($"No SuppressFromVsix items found under '{vsSdkTargetsDirectory}'.");
        }
        return suppressed;
    }

    private static HashSet<string> ReadExplicitlyIncludedPackages(string vsixProjectPath) =>
        ExplicitPackageInclude.Matches(File.ReadAllText(vsixProjectPath))
            .Cast<Match>()
            .Select(match => match.Groups["id"].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string Key(AssemblyName assembly) => $"{assembly.Name}|{assembly.Version}";
}
