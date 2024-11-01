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
        threadHandling = new NoOpThreadHandler();

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
        MefTestHelpers.CheckTypeCanBeImported<EmbeddedDotnetAnalyzerProvider, IEnterpriseRoslynAnalyzerProvider>(
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
    public void Ctor_CreatesLoader()
    {
        var factory = Substitute.For<IAnalyzerAssemblyLoaderFactory>();
        
        new EmbeddedDotnetAnalyzerProvider(default, factory, default, default, default);

        factory.Received(1).Create();
    }

    [TestMethod]
    public void GetBasicAsync_GetsAnalyzersFromExpectedLocation()
    {
        testSubject.GetBasicAsync();

        locator.Received(1).GetBasicAnalyzerFullPaths();
    }
    
    [TestMethod]
    public void GetBasicAsync_RunsOnBackgroundThread()
    {
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var subject = new EmbeddedDotnetAnalyzerProvider(default,
            Substitute.For<IAnalyzerAssemblyLoaderFactory>(),
            default,
            default,
            threadHandlingMock);

        subject.GetBasicAsync();
        
        threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<ImmutableArray<AnalyzerFileReference>>>>());
    }
    
    [TestMethod]
    public void GetEnterpriseOrNullAsync_GetsAnalyzersFromExpectedLocation()
    {
        testSubject.GetEnterpriseOrNullAsync("scope");

        locator.Received(1).GetEnterpriseAnalyzerFullPaths();
    }
    
    [TestMethod]
    public void GetEnterpriseOrNullAsync_RunsOnBackgroundThread()
    {
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        var subject = new EmbeddedDotnetAnalyzerProvider(default,
            Substitute.For<IAnalyzerAssemblyLoaderFactory>(),
            default,
            default,
            threadHandlingMock);

        subject.GetEnterpriseOrNullAsync("scope");
        
        threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<ImmutableArray<AnalyzerFileReference>?>>>());
    }

    [TestMethod]
    public async Task GetBasicAsync_AnalyzerFilesExist_ReturnsAnalyzerFileReference()
    {
        locator.GetBasicAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll"), GetAnalyzerFullPath("analyzer2.dll")]);

        var analyzerFileReferences = await testSubject.GetBasicAsync();
        
        analyzerFileReferences.Should().NotBeNull();
        analyzerFileReferences.Length.Should().Be(2);
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences, GetAnalyzerFullPath("analyzer1.dll"));
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences, GetAnalyzerFullPath("analyzer2.dll"));
    }
    
    [TestMethod]
    public async Task GetEnterpriseOrNullAsync_AnalyzerFilesExist_ReturnsAnalyzerFileReference()
    {
        const string configurationScopeId = "scope";
        locator.GetEnterpriseAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll"), GetAnalyzerFullPath("analyzer2.dll")]);
        indicator.ShouldUseEnterpriseCSharpAnalyzerAsync(configurationScopeId).Returns(true);

        var analyzerFileReferences = await testSubject.GetEnterpriseOrNullAsync(configurationScopeId);
        
        analyzerFileReferences.Should().NotBeNull();
        analyzerFileReferences!.Value.Length.Should().Be(2);
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences.Value, GetAnalyzerFullPath("analyzer1.dll"));
        ContainsExpectedAnalyzerFileReference(analyzerFileReferences.Value, GetAnalyzerFullPath("analyzer2.dll"));
    }
    
    [TestMethod]
    public async Task GetEnterpriseOrNullAsync_AnalyzerFilesExist_NotEnabledForConfigScope_ReturnsAnalyzerFileReference()
    {
        const string configurationScopeId = "scope";
        locator.GetEnterpriseAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll"), GetAnalyzerFullPath("analyzer2.dll")]);
        indicator.ShouldUseEnterpriseCSharpAnalyzerAsync(configurationScopeId).Returns(false);

        var analyzerFileReferences = await testSubject.GetEnterpriseOrNullAsync(configurationScopeId);
        
        analyzerFileReferences.Should().BeNull();
    }

    [TestMethod]
    public async Task GetBasicAsync_AnalyzerFilesDoNotExist_ReturnsLogsAndThrows()
    {
        locator.GetBasicAnalyzerFullPaths().Returns([]);

        Func<Task> act = () => testSubject.GetBasicAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(Resources.EmbeddedRoslynAnalyzersNotFound);
        logger.Received(1).LogVerbose(Resources.EmbeddedRoslynAnalyzersNotFound);
    }
    
    [TestMethod]
    public async Task GetEnterpriseOrNullAsync_AnalyzerFilesDoNotExist_ReturnsLogsAndThrows()
    {
        locator.GetEnterpriseAnalyzerFullPaths().Returns([]);

        Func<Task> act = () => testSubject.GetEnterpriseOrNullAsync("scope");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(Resources.EmbeddedRoslynAnalyzersNotFound);
        logger.Received(1).LogVerbose(Resources.EmbeddedRoslynAnalyzersNotFound);
    }

    [TestMethod]
    public async Task GetBasicAsync_CachesAnalyzerFileReferences()
    {
        await testSubject.GetBasicAsync();
        await testSubject.GetBasicAsync();

        loaderFactory.Received(1).Create();
        locator.Received(1).GetBasicAnalyzerFullPaths();
    }
    
    [TestMethod]
    public async Task GetEnterpriseOrNullAsync_CachesAnalyzerFileReferences()
    {
        await testSubject.GetEnterpriseOrNullAsync("scope");
        await testSubject.GetEnterpriseOrNullAsync("other scope");

        loaderFactory.Received(1).Create();
        locator.Received(1).GetEnterpriseAnalyzerFullPaths();
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
        indicator.ShouldUseEnterpriseCSharpAnalyzerAsync(default).ReturnsForAnyArgs(true);
        locator.GetBasicAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll")]);
        locator.GetEnterpriseAnalyzerFullPaths().Returns([GetAnalyzerFullPath("analyzer1.dll")]);
        loaderFactory.Create().Returns(analyzerAssemblyLoader);
    }
}
