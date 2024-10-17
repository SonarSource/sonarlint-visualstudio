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
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class EmbeddedRoslynAnalyzerProviderTests
{
    private const string AnalyzersPath = "C:\\somepath";
    private readonly IAnalyzerAssemblyLoader analyzerAssemblyLoader = Substitute.For<IAnalyzerAssemblyLoader>();
    private EmbeddedRoslynAnalyzerProvider testSubject;
    private IEmbeddedRoslynAnalyzersLocator locator;
    private IAnalyzerAssemblyLoaderFactory analyzerAssemblyLoaderFactory;
    private ILogger logger;

    [TestInitialize]
    public void TestInitialize()
    {
        locator = Substitute.For<IEmbeddedRoslynAnalyzersLocator>();
        analyzerAssemblyLoaderFactory = Substitute.For<IAnalyzerAssemblyLoaderFactory>();
        logger = Substitute.For<ILogger>();

        testSubject = new EmbeddedRoslynAnalyzerProvider(locator, analyzerAssemblyLoaderFactory, logger);
        MockServices();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<EmbeddedRoslynAnalyzerProvider, IEmbeddedRoslynAnalyzerProvider>(
            MefTestHelpers.CreateExport<IEmbeddedRoslynAnalyzersLocator>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_IsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<EmbeddedRoslynAnalyzerProvider>();
    }

    [TestMethod]
    public void Get_GetsAnalyzersFromExpectedLocation()
    {
        testSubject.Get();

        locator.Received(1).GetAnalyzerFullPaths();
    }

    [TestMethod]
    public void Get_AnalyzerFilesExist_ReturnsAnalyzerFileReference()
    {
        locator.GetAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll"), GetAnalyzerFullPath("analyzer2.dll")]);

        var analyzerFileReferences = testSubject.Get();

        analyzerAssemblyLoaderFactory.Received(1).Create();
        analyzerFileReferences.Should().NotBeNull();
        analyzerFileReferences.Length.Should().Be(2);
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences, GetAnalyzerFullPath("analyzer1.dll"));
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences, GetAnalyzerFullPath("analyzer2.dll"));
    }

    [TestMethod]
    public void Get_AnalyzerFilesDoNotExist_ReturnsLogsAndThrows()
    {
        locator.GetAnalyzerFullPaths().Returns([]);

        Action act = () => testSubject.Get();

        act.Should().Throw<InvalidOperationException>().WithMessage(Resources.EmbeddedRoslynAnalyzersNotFound);
        logger.Received(1).LogVerbose(Resources.EmbeddedRoslynAnalyzersNotFound);
        analyzerAssemblyLoaderFactory.DidNotReceive().Create();
    }

    [TestMethod]
    public void Get_CachesAnalyzerFileReferences()
    {
        testSubject.Get();
        testSubject.Get();

        analyzerAssemblyLoaderFactory.Received(1).Create();
        locator.Received(1).GetAnalyzerFullPaths();
    }

    private static string GetAnalyzerFullPath(string analyzerFile)
    {
        return Path.Combine(AnalyzersPath, analyzerFile);
    }

    private static void ContainsExpectedAnalyzerFileReference(
        ImmutableArray<AnalyzerFileReference> analyzerFileReference,
        string analyzerPath)
    {
        analyzerFileReference.Should().Contain(analyzerFile => analyzerFile.FullPath == analyzerPath);
    }

    private void MockServices()
    {
        locator.GetAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll")]);
        analyzerAssemblyLoaderFactory.Create().Returns(analyzerAssemblyLoader);
    }
}
