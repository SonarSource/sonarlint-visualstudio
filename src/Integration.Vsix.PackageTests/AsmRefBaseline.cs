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

namespace SonarLint.VisualStudio.Integration.Vsix.PackageTests;

internal sealed class AsmRefReference(AssemblyName identity, IReadOnlyList<string> referencedBy)
{
    public AssemblyName Identity { get; } = identity;
    public IReadOnlyList<string> ReferencedBy { get; } = referencedBy;
}

internal sealed class AsmRefBaseline
{
    private const string SourcePrefix = "Assembly: '";
    private const string ReferencePrefix = "- '";

    internal AsmRefBaseline(IReadOnlyList<AssemblyName> sources, IReadOnlyList<AsmRefReference> references)
    {
        Sources = sources;
        References = references;
    }

    public IReadOnlyList<AssemblyName> Sources { get; }
    public IReadOnlyList<AsmRefReference> References { get; }

    public static string GetPath(string vsixProjectDirectory, bool strongNamed) =>
        Path.Combine(vsixProjectDirectory, $"AsmRef_Integration.Vsix_Baseline_{(strongNamed ? "WithStrongNames" : "WithoutStrongNames")}.txt");

    public static AsmRefBaseline Load(string path)
    {
        var sources = new List<AssemblyName>();
        var referrersByReference = new Dictionary<string, List<string>>();
        AssemblyName currentSource = null;

        foreach (var line in File.ReadLines(path))
        {
            if (line.StartsWith(SourcePrefix, StringComparison.Ordinal))
            {
                currentSource = ParseQuotedName(line, SourcePrefix);
                sources.Add(currentSource);
            }
            else if (currentSource != null && line.StartsWith(ReferencePrefix, StringComparison.Ordinal))
            {
                var reference = ParseQuotedName(line, ReferencePrefix);
                if (!referrersByReference.TryGetValue(reference.FullName, out var referrers))
                {
                    referrersByReference[reference.FullName] = referrers = [];
                }
                referrers.Add(currentSource.Name);
            }
        }

        var references = referrersByReference
            .Select(x => new AsmRefReference(new AssemblyName(x.Key), x.Value))
            .ToList();
        return new AsmRefBaseline(sources, references);
    }

    private static AssemblyName ParseQuotedName(string line, string prefix) =>
        new(line.Substring(prefix.Length).TrimEnd().TrimEnd('\''));
}
