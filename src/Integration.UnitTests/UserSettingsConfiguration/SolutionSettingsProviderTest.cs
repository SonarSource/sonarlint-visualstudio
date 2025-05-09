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
public class SolutionSettingsProviderTest
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
        MefTestHelpers.CheckTypeCanBeImported<SolutionSettingsProvider, ISolutionSettingsProvider>(
            MefTestHelpers.CreateExport<ISolutionSettingsStorage>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SolutionSettingsProvider>();

    [TestMethod]
    public void UpdateSolutionFileExclusions_UpdatesSolutionSettings()
    {
        SetupSolutionSettings(new SolutionAnalysisSettings());
        var testSubject = CreateAndInitializeTestSubject();
        string[] exclusions = ["1", "two", "3"];

        testSubject.UpdateSolutionFileExclusions(exclusions);

        solutionSettingsStorage.Received(1).SaveSettingsFile(Arg.Is<SolutionAnalysisSettings>(x => x.UserDefinedFileExclusions.SequenceEqual(exclusions, default)));
    }

    [TestMethod]
    public void UpdateAnalysisProperties_UpdatesSolutionSettings()
    {
        var exclusions = ImmutableArray.Create("file1");
        SetupSolutionSettings(new SolutionAnalysisSettings(ImmutableDictionary<string, string>.Empty, exclusions));
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.UpdateAnalysisProperties(new Dictionary<string, string> { ["prop"] = "value" });

        solutionSettingsStorage.Received(1).SaveSettingsFile(Arg.Is<SolutionAnalysisSettings>(x =>
            x.AnalysisProperties.Count == 1
            && x.AnalysisProperties["prop"] == "value"
            && x.UserDefinedFileExclusions.Length == 1
            && x.UserDefinedFileExclusions[0] == "file1"));
    }

    private SolutionSettingsProvider CreateAndInitializeTestSubject()
    {
        processorFactory = MockableInitializationProcessor.CreateFactory<SolutionSettingsProvider>(threadHandling, testLogger);
        var testSubject = new SolutionSettingsProvider(solutionSettingsStorage, userSettingsProvider, processorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private void SetupSolutionSettings(SolutionAnalysisSettings solutionAnalysisSettings)
    {
        var analysisSettings = new AnalysisSettings(solutionFileExclusions: solutionAnalysisSettings.UserDefinedFileExclusions,
            analysisProperties: solutionAnalysisSettings.AnalysisProperties);
        userSettingsProvider.UserSettings.Returns(new UserSettings(analysisSettings, string.Empty));
    }
}
