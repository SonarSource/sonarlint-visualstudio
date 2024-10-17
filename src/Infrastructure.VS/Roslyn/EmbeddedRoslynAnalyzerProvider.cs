﻿/*
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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO.Abstractions;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

[Export(typeof(IEmbeddedRoslynAnalyzerProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class EmbeddedRoslynAnalyzerProvider : IEmbeddedRoslynAnalyzerProvider
{
    private readonly IEmbeddedRoslynAnalyzersLocator locator;
    private readonly IAnalyzerAssemblyLoaderFactory analyzerAssemblyLoaderFactory;
    private readonly ILogger logger;
    private readonly IFileSystem fileSystem;

    [ImportingConstructor]
    public EmbeddedRoslynAnalyzerProvider(IEmbeddedRoslynAnalyzersLocator locator, ILogger logger) :
        this(locator, new AnalyzerAssemblyLoaderFactory(), logger, new FileSystem())
    {
    }

    internal EmbeddedRoslynAnalyzerProvider(IEmbeddedRoslynAnalyzersLocator locator,
        IAnalyzerAssemblyLoaderFactory analyzerAssemblyLoaderFactory,
        ILogger logger,
        IFileSystem fileSystem)
    {
        this.locator = locator;
        this.analyzerAssemblyLoaderFactory = analyzerAssemblyLoaderFactory;
        this.logger = logger;
        this.fileSystem = fileSystem;
    }

    public ImmutableArray<AnalyzerFileReference> Get()
    {
        var analyzerPaths = fileSystem.Directory.GetFiles(locator.GetPathToParentFolder());
        if(analyzerPaths.Length == 0)
        {
            logger.WriteLine(Resources.EmbeddedRoslynAnalyzersNotFound);
            return ImmutableArray<AnalyzerFileReference>.Empty;
        }
        var loader = analyzerAssemblyLoaderFactory.Create();
        return analyzerPaths.Select(path => new AnalyzerFileReference(path, loader)).ToImmutableArray();
    }
}
