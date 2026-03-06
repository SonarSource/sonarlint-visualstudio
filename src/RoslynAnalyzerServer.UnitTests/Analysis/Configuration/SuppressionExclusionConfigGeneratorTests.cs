/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class SuppressionExclusionConfigGeneratorTests
{
    private const string AppDataRoot = @"C:\Users\Test\AppData\Roaming";
    private static readonly string ExpectedConfigFilePath = Path.Combine(AppDataRoot, "SonarLint for Visual Studio", SuppressionExclusionConfigGenerator.ConfigFileName);

    private IRoslynAnalyzerAssemblyContentsLoader roslynAnalyzerAssemblyContentsLoader = null!;
    private IEnvironmentVariableProvider environmentVariableProvider = null!;
    private IFileSystemService fileSystemService = null!;
    private IInitializationProcessorFactory initializationProcessorFactory = null!;
    private MockableInitializationProcessor createdInitializationProcessor = null!;
    private TaskCompletionSource<byte> initializationBarrier = null!;
    private ILogger logger = null!;
    private SuppressionExclusionConfigGenerator testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        initializationBarrier = new TaskCompletionSource<byte>();
        roslynAnalyzerAssemblyContentsLoader = Substitute.For<IRoslynAnalyzerAssemblyContentsLoader>();
        environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
        environmentVariableProvider.GetFolderPath(Environment.SpecialFolder.ApplicationData).Returns(AppDataRoot);
        fileSystemService = Substitute.For<IFileSystemService>();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<SuppressionExclusionConfigGenerator>(
            new NoOpThreadHandler(),
            new TestLogger(),
            processor =>
            {
                MockableInitializationProcessor.ConfigureWithWait(processor, initializationBarrier);
                createdInitializationProcessor = processor;
            });

        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SuppressionExclusionConfigGenerator, ISuppressionExclusionConfigGenerator>(
            MefTestHelpers.CreateExport<IRoslynAnalyzerAssemblyContentsLoader>(),
            MefTestHelpers.CreateExport<IEnvironmentVariableProvider>(),
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<SuppressionExclusionConfigGenerator>();

    [TestMethod]
    public void Ctor_SetsConfigFilePath()
    {
        testSubject.ConfigFilePath.Should().Be(ExpectedConfigFilePath);
    }

    [TestMethod]
    public void Ctor_SetsLogContext()
    {
        logger.Received(1).ForContext(Resources.SuppressionExclusionConfigGenerator_LogContext);
    }

    [TestMethod]
    public void Initialization_PassesCorrectDependenciesToFactory()
    {
        initializationProcessorFactory.Received(1).Create<SuppressionExclusionConfigGenerator>(
            Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x =>
                x.Count == 1 &&
                x.Contains(roslynAnalyzerAssemblyContentsLoader)),
            Arg.Any<Func<IThreadHandling, Task>>());
    }

    [TestMethod]
    public void GenerateConfiguration_WritesCorrectConfigFile()
    {
        var ruleKeys = ImmutableHashSet.Create("S1234", "S5678");
        roslynAnalyzerAssemblyContentsLoader.GetAllSupportedRuleKeys().Returns(ruleKeys);

        CompleteInitialization();

        fileSystemService.Directory.Received(1).CreateDirectory(Path.GetDirectoryName(ExpectedConfigFilePath));
        fileSystemService.File.Received(1).WriteAllText(
            ExpectedConfigFilePath,
            Arg.Is<string>(content =>
                content.Contains("is_global = true") &&
                content.Contains("global_level = 1999999999") &&
                content.Contains("dotnet_remove_unnecessary_suppression_exclusions = ") &&
                ContainsAllRuleKeys(content, ruleKeys)));
    }

    [TestMethod]
    public void GenerateConfiguration_EmptyRuleKeys_WritesConfigWithEmptyExclusions()
    {
        roslynAnalyzerAssemblyContentsLoader.GetAllSupportedRuleKeys().Returns(ImmutableHashSet<string>.Empty);

        CompleteInitialization();

        fileSystemService.File.Received(1).WriteAllText(
            ExpectedConfigFilePath,
            Arg.Is<string>(content =>
                content.Contains("dotnet_remove_unnecessary_suppression_exclusions = ")));
    }

    [TestMethod]
    public void GenerateConfiguration_NonCriticalException_CatchesAndLogs()
    {
        roslynAnalyzerAssemblyContentsLoader.GetAllSupportedRuleKeys().Throws(new InvalidOperationException("test error"));

        var act = () => CompleteInitialization();

        act.Should().NotThrow();
        logger.Received(1).WriteLine(Resources.SuppressionExclusionConfigGenerator_FailedToWrite, Arg.Any<InvalidOperationException>());
    }

    [TestMethod]
    public void GenerateConfiguration_CriticalException_Throws()
    {
        roslynAnalyzerAssemblyContentsLoader.GetAllSupportedRuleKeys().Throws(new StackOverflowException());

        var act = () => CompleteInitialization();

        act.Should().Throw<StackOverflowException>();
    }

    [TestMethod]
    public void GenerateConfiguration_BeforeInitialization_DoesNotWriteFile()
    {
        roslynAnalyzerAssemblyContentsLoader.GetAllSupportedRuleKeys().Returns(ImmutableHashSet.Create("S1234"));

        fileSystemService.File.DidNotReceiveWithAnyArgs().WriteAllText(default, default);
    }

    private static bool ContainsAllRuleKeys(string content, ImmutableHashSet<string> ruleKeys)
    {
        foreach (var key in ruleKeys)
        {
            if (!content.Contains(key))
            {
                return false;
            }
        }
        return true;
    }

    private void CompleteInitialization()
    {
        initializationBarrier.TrySetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }

    private SuppressionExclusionConfigGenerator CreateTestSubject() =>
        new(roslynAnalyzerAssemblyContentsLoader,
            environmentVariableProvider,
            fileSystemService,
            initializationProcessorFactory,
            logger);
}
