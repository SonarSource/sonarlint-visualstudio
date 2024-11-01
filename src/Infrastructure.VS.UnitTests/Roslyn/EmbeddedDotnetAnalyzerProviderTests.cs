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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class EmbeddedDotnetAnalyzerProviderTests
{
    private const string AnalyzersPath = "C:\\somepath";
    private readonly IAnalyzerAssemblyLoader analyzerAssemblyLoader = Substitute.For<IAnalyzerAssemblyLoader>();
    private EmbeddedDotnetAnalyzerProvider testSubject;
    private IEmbeddedDotnetAnalyzersLocator locator;
    private IAnalyzerAssemblyLoaderFactory loaderFactory;
    private IConfigurationScopeDotnetAnalyzerIndicator indicator;
    private ILogger logger;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        locator = Substitute.For<IEmbeddedDotnetAnalyzersLocator>();
        loaderFactory = Substitute.For<IAnalyzerAssemblyLoaderFactory>();
        loaderFactory.Create().Returns(analyzerAssemblyLoader);
        logger = Substitute.For<ILogger>();
        indicator = Substitute.For<IConfigurationScopeDotnetAnalyzerIndicator>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        testSubject = new EmbeddedDotnetAnalyzerProvider(locator, loaderFactory, indicator, logger, threadHandling);
        MockServices();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<EmbeddedDotnetAnalyzerProvider, IBasicRoslynAnalyzerProvider>(
            MefTestHelpers.CreateExport<IEmbeddedDotnetAnalyzersLocator>(),
            MefTestHelpers.CreateExport<IAnalyzerAssemblyLoaderFactory>(),
            MefTestHelpers.CreateExport<IConfigurationScopeDotnetAnalyzerIndicator>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_IsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<EmbeddedDotnetAnalyzerProvider>();
    }

    [TestMethod]
    public void Get_GetsAnalyzersFromExpectedLocation()
    {
        testSubject.GetAsync();

        locator.Received(1).GetBasicAnalyzerFullPaths();
    }

    [TestMethod]
    public async Task Get_AnalyzerFilesExist_ReturnsAnalyzerFileReference()
    {
        locator.GetBasicAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll"), GetAnalyzerFullPath("analyzer2.dll")]);

        var analyzerFileReferences = await testSubject.GetAsync();

        loaderFactory.Received(1).Create();
        analyzerFileReferences.Should().NotBeNull();
        analyzerFileReferences.Length.Should().Be(2);
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences, GetAnalyzerFullPath("analyzer1.dll"));
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences, GetAnalyzerFullPath("analyzer2.dll"));
    }

    [TestMethod]
    public async Task Get_AnalyzerFilesDoNotExist_ReturnsLogsAndThrows()
    {
        locator.GetBasicAnalyzerFullPaths().Returns([]);

        Func<Task> act = () => testSubject.GetAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(Resources.EmbeddedRoslynAnalyzersNotFound);
        logger.Received(1).LogVerbose(Resources.EmbeddedRoslynAnalyzersNotFound);
    }

    [TestMethod]
    public async Task Get_CachesAnalyzerFileReferences()
    {
        await testSubject.GetAsync();
        await testSubject.GetAsync();

        loaderFactory.Received(1).Create();
        locator.Received(1).GetBasicAnalyzerFullPaths();
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
        locator.GetBasicAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll")]);
        loaderFactory.Create().Returns(analyzerAssemblyLoader);
    }
}
