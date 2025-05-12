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

using System.Collections.Immutable;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.UserSettingsConfiguration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.UserSettingsConfiguration;

[TestClass]
public class SolutionUserSettingsUpdaterTest
{
    private IInitializationProcessorFactory processorFactory;
    private TestLogger testLogger;
    private IThreadHandling threadHandling;
    private IUserSettingsProvider userSettingsProvider;
    private ISolutionSettingsStorage solutionSettingsStorage;

    [TestInitialize]
    public void Initialize()
    {
        testLogger = new TestLogger();
        userSettingsProvider = Substitute.For<IUserSettingsProvider>();
        solutionSettingsStorage = Substitute.For<ISolutionSettingsStorage>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SolutionUserSettingsUpdater, ISolutionUserSettingsUpdater>(
            MefTestHelpers.CreateExport<ISolutionSettingsStorage>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SolutionUserSettingsUpdater>();

    [TestMethod]
    public void Initialization_SubscribesToEvents()
    {
        var dependencies = new IRequireInitialization[] { solutionSettingsStorage, userSettingsProvider };

        var testSubject = CreateAndInitializeTestSubject();
        var initializationProcessor = testSubject.InitializationProcessor;

        Received.InOrder(() =>
        {
            processorFactory.Create<SolutionUserSettingsUpdater>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(collection => collection.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            initializationProcessor.InitializeAsync();
            initializationProcessor.InitializeAsync();
        });
    }

    [TestMethod]
    public async Task UpdateFileExclusions_UpdatesSolutionSettings()
    {
        SetupUserSettings(new SolutionAnalysisSettings());
        var testSubject = CreateAndInitializeTestSubject();
        string[] exclusions = ["1", "two", "3"];

        await testSubject.UpdateFileExclusions(exclusions);

        solutionSettingsStorage.Received(1).SaveSettingsFile(Arg.Is<SolutionAnalysisSettings>(x => x.UserDefinedFileExclusions.SequenceEqual(exclusions, default)));
    }

    [TestMethod]
    public async Task UpdateAnalysisProperties_UpdatesSolutionSettings()
    {
        var exclusions = ImmutableArray.Create("file1");
        SetupUserSettings(new SolutionAnalysisSettings(ImmutableDictionary<string, string>.Empty, exclusions));
        var testSubject = CreateAndInitializeTestSubject();

        await testSubject.UpdateAnalysisProperties(new Dictionary<string, string> { ["prop"] = "value" });

        solutionSettingsStorage.Received(1).SaveSettingsFile(Arg.Is<SolutionAnalysisSettings>(x =>
            x.AnalysisProperties.Count == 1
            && x.AnalysisProperties["prop"] == "value"
            && x.UserDefinedFileExclusions.Length == 1
            && x.UserDefinedFileExclusions[0] == "file1"));
    }

    [TestMethod]
    public void FileExclusions_ReturnsSolutionFileExclusions()
    {
        var settings = new SolutionAnalysisSettings(analysisProperties: ImmutableDictionary<string, string>.Empty, fileExclusions: ImmutableArray.Create<string>("*.ts,*.js"));
        SetupUserSettings(settings);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.FileExclusions.Should().BeEquivalentTo(userSettingsProvider.UserSettings.AnalysisSettings.SolutionFileExclusions);
    }

    private SolutionUserSettingsUpdater CreateAndInitializeTestSubject()
    {
        processorFactory = MockableInitializationProcessor.CreateFactory<SolutionUserSettingsUpdater>(threadHandling, testLogger);
        var testSubject = new SolutionUserSettingsUpdater(solutionSettingsStorage, userSettingsProvider, processorFactory, threadHandling);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private void SetupUserSettings(SolutionAnalysisSettings solutionAnalysisSettings)
    {
        var analysisSettings = new AnalysisSettings(solutionFileExclusions: solutionAnalysisSettings.UserDefinedFileExclusions,
            analysisProperties: solutionAnalysisSettings.AnalysisProperties);
        userSettingsProvider.UserSettings.Returns(new UserSettings(analysisSettings, string.Empty));
    }
}
