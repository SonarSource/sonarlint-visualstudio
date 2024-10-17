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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.EmbeddedAnalyzers;

[Export(typeof(IEmbeddedRoslynAnalyzersLocator))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class EmbeddedRoslynAnalyzersLocator : IEmbeddedRoslynAnalyzersLocator
{
    private const string PathInsideVsix = "EmbeddedRoslynAnalyzers";
    private const string DllsSearchPattern = "*.dll";

    private readonly IFileSystem fileSystem;
    private readonly IVsixRootLocator vsixRootLocator;

    [ImportingConstructor]
    public EmbeddedRoslynAnalyzersLocator(IVsixRootLocator vsixRootLocator) : this(vsixRootLocator, new FileSystem())
    {
    }

    internal EmbeddedRoslynAnalyzersLocator(IVsixRootLocator vsixRootLocator, IFileSystem fileSystem)
    {
        this.vsixRootLocator = vsixRootLocator;
        this.fileSystem = fileSystem;
    }

    public string GetPathToParentFolder()
    {
        return Path.Combine(vsixRootLocator.GetVsixRoot(), PathInsideVsix);
    }

    public List<string> GetAnalyzerFullPaths()
    {
       return fileSystem.Directory.GetFiles(GetPathToParentFolder(), DllsSearchPattern).ToList();
    }
}
