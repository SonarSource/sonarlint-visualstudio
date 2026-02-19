/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests;

[TestClass]
public class RoslynEnvironmentInitializerTests
{
    private IRoslynAnalysisHttpServer roslynAnalysisHttpServer = null!;
    private IRoslynAnalyzerAssemblyContentsLoader analyzerAssemblyContentsLoader = null!;
    private ISuppressionExclusionConfigGenerator suppressionExclusionConfigGenerator = null!;
    private IInitializationProcessorFactory initializationProcessorFactory = null!;
    private MockableInitializationProcessor createdInitializationProcessor = null!;
    private TaskCompletionSource<byte> initializationBarrier = null!;
    private RoslynEnvironmentInitializer testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        initializationBarrier = new TaskCompletionSource<byte>();
        roslynAnalysisHttpServer = Substitute.For<IRoslynAnalysisHttpServer>();
        analyzerAssemblyContentsLoader = Substitute.For<IRoslynAnalyzerAssemblyContentsLoader>();
        suppressionExclusionConfigGenerator = Substitute.For<ISuppressionExclusionConfigGenerator>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<RoslynEnvironmentInitializer>(
            new NoOpThreadHandler(),
            new TestLogger(),
            processor =>
            {
                MockableInitializationProcessor.ConfigureWithWait(processor, initializationBarrier);
                createdInitializationProcessor = processor;
            });
        testSubject = new RoslynEnvironmentInitializer(
            roslynAnalysisHttpServer,
            analyzerAssemblyContentsLoader,
            suppressionExclusionConfigGenerator,
            initializationProcessorFactory);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynEnvironmentInitializer, IRoslynEnvironmentInitializer>(
            MefTestHelpers.CreateExport<IRoslynAnalysisHttpServer>(),
            MefTestHelpers.CreateExport<IRoslynAnalyzerAssemblyContentsLoader>(),
            MefTestHelpers.CreateExport<ISuppressionExclusionConfigGenerator>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynEnvironmentInitializer>();

    [TestMethod]
    public void Initialization_PassesCorrectDependenciesToFactory()
    {
        initializationProcessorFactory.Received(1).Create<RoslynEnvironmentInitializer>(
            Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x =>
                x.Count == 2 &&
                x.Contains(analyzerAssemblyContentsLoader) &&
                x.Contains(suppressionExclusionConfigGenerator)),
            Arg.Any<Func<IThreadHandling, Task>>());
    }

    [TestMethod]
    public void Initialization_StartsHttpServerAfterInitialization()
    {
        roslynAnalysisHttpServer.DidNotReceive().StartListenAsync();

        CompleteInitialization();

        roslynAnalysisHttpServer.Received(1).StartListenAsync();
    }

    [TestMethod]
    public void Dispose_DisposesHttpServer()
    {
        testSubject.Dispose();

        roslynAnalysisHttpServer.Received(1).Dispose();
    }

    private void CompleteInitialization()
    {
        initializationBarrier.TrySetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }
}
