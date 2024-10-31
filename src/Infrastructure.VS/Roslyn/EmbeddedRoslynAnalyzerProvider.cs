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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

[Export(typeof(IEmbeddedRoslynAnalyzerProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class EmbeddedRoslynAnalyzerProvider : IEmbeddedRoslynAnalyzerProvider
{
    private readonly IEmbeddedDotnetAnalyzersLocator locator;
    private readonly IAnalyzerAssemblyLoaderFactory analyzerAssemblyLoaderFactory;
    private readonly ILogger logger;
    private IAnalyzerReferencesHolder embeddedAnalyzers;

    [ImportingConstructor]
    public EmbeddedRoslynAnalyzerProvider(IEmbeddedDotnetAnalyzersLocator locator, ILogger logger) :
        this(locator, new AnalyzerAssemblyLoaderFactory(), logger)
    {
    }

    internal EmbeddedRoslynAnalyzerProvider(IEmbeddedDotnetAnalyzersLocator locator,
        IAnalyzerAssemblyLoaderFactory analyzerAssemblyLoaderFactory,
        ILogger logger)
    {
        this.locator = locator;
        this.analyzerAssemblyLoaderFactory = analyzerAssemblyLoaderFactory;
        this.logger = logger;
    }

    public IAnalyzerReferencesHolder Get()
    {
        embeddedAnalyzers ??= CreateAnalyzerFileReferences();

        return embeddedAnalyzers;
    }

    private IAnalyzerReferencesHolder CreateAnalyzerFileReferences()
    {
        var analyzerPaths = locator.GetBasicAnalyzerFullPaths();
        if(analyzerPaths.Count == 0)
        {
            logger.LogVerbose(Resources.EmbeddedRoslynAnalyzersNotFound);
            throw new InvalidOperationException(Resources.EmbeddedRoslynAnalyzersNotFound);
        }
        var loader = analyzerAssemblyLoaderFactory.Create();
        return new AnalyzerReferencesHolder(analyzerPaths, loader);
    }
}
