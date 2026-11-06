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

using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix.PackageTests;

[TestClass]
public class VsixPackageTests
{
    private static VsixPackage vsix;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) => vsix = VsixPackage.Open(BuildPaths.Vsix);

    [ClassCleanup]
    public static void ClassCleanup() => vsix?.Dispose();

    [TestMethod]
    public void AsmRefAssemblies_ArePackagedWithRecordedIdentity()
    {
        var baseline = AsmRefBaseline.Load(AsmRefBaseline.GetPath(BuildPaths.VsixProjectDirectory, vsix.AreSonarLintAssembliesStrongNamed));
        var resolver = ShippedPackageResolver.Load(BuildPaths.VsixProjectDirectory, BuildPaths.NuGetPackageRoot);
        var packaged = vsix.Assemblies
            .Where(x => string.IsNullOrEmpty(x.Name.CultureName))
            .ToDictionary(x => x.Name.Name, x => x.Name, StringComparer.OrdinalIgnoreCase);

        var problems = FindSourceProblems(baseline, packaged).Concat(FindReferenceProblems(baseline, packaged, resolver));

        problems.Should().BeEmpty();
    }

    [TestMethod]
    public void PackagedAssemblies_AreStrongNamed()
    {
        if (!vsix.AreSonarLintAssembliesStrongNamed)
        {
            Assert.Inconclusive("The VSIX was built without strong names (SignArtifacts is not set).");
        }

        vsix.Assemblies.Where(x => !x.Name.IsStrongNamed()).Select(x => x.PathInVsix).Should().BeEmpty();
    }

    private static IEnumerable<string> FindSourceProblems(AsmRefBaseline baseline, Dictionary<string, AssemblyName> packaged)
    {
        foreach (var source in baseline.Sources)
        {
            if (!packaged.TryGetValue(source.Name, out var actual))
            {
                yield return $"'{source.Name}' is in the AsmRef baseline but not in the VSIX";
            }
            else if (!source.HasSameIdentity(actual, versionFieldCount: 3))
            {
                yield return $"The AsmRef baseline expects '{source.FullName}' but the VSIX contains '{actual.FullName}'";
            }
        }
    }

    private static IEnumerable<string> FindReferenceProblems(AsmRefBaseline baseline, Dictionary<string, AssemblyName> packaged, ShippedPackageResolver resolver)
    {
        foreach (var reference in baseline.References)
        {
            var expected = reference.Identity;
            var referencedBy = string.Join(", ", reference.ReferencedBy.Distinct().OrderBy(x => x));
            if (packaged.TryGetValue(expected.Name, out var actual))
            {
                if (!expected.HasSameIdentity(actual, versionFieldCount: 4))
                {
                    yield return $"{referencedBy} references '{expected.FullName}' but the VSIX contains '{actual.FullName}'";
                }
            }
            else if (resolver.FindPackageToShip(expected) is { } package)
            {
                yield return $"{referencedBy} references '{expected.FullName}' from NuGet package {package}, which Visual Studio doesn't provide, but it is not in the VSIX";
            }
        }
    }
}
