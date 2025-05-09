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
public class GlobalSettingsProviderTest
{
    private IInitializationProcessorFactory processorFactory;
    private TestLogger testLogger;
    private IThreadHandling threadHandling;
    private IUserSettingsProvider userSettingsProvider;
    private IGlobalSettingsStorage globalSettingsStorage;

    [TestInitialize]
    public void Initialize()
    {
        testLogger = new TestLogger();
        userSettingsProvider = Substitute.For<IUserSettingsProvider>();
        globalSettingsStorage = Substitute.For<IGlobalSettingsStorage>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<GlobalSettingsProvider, IGlobalSettingsProvider>(
            MefTestHelpers.CreateExport<IGlobalSettingsStorage>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<GlobalSettingsProvider>();

    [TestMethod]
    public void DisableRule_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        SetupUserSettings(new GlobalAnalysisSettings());
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.DisableRule("somerule");

        globalSettingsStorage.Received(1).SaveSettingsFile(Arg.Is<GlobalAnalysisSettings>(x => x.Rules.ContainsKey("somerule") && x.Rules["somerule"].Level == RuleLevel.Off));
    }

    [TestMethod]
    public void DisableRule_EnabledRule_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        SetupUserSettings(new GlobalAnalysisSettings(rules: ImmutableDictionary.Create<string, RuleConfig>().Add("somerule", new RuleConfig(RuleLevel.On)), ImmutableArray<string>.Empty));
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.DisableRule("somerule");

        globalSettingsStorage.Received(1).SaveSettingsFile(Arg.Is<GlobalAnalysisSettings>(x => x.Rules.ContainsKey("somerule") && x.Rules["somerule"].Level == RuleLevel.Off));
    }

    [TestMethod]
    public void DisableRule_OtherRuleNotDisabled_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        SetupUserSettings(new GlobalAnalysisSettings(rules: ImmutableDictionary.Create<string, RuleConfig>().Add("someotherrule", new RuleConfig(RuleLevel.On)), ImmutableArray<string>.Empty));
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.DisableRule("somerule");

        globalSettingsStorage.Received(1).SaveSettingsFile(
            Arg.Is<GlobalAnalysisSettings>(x =>
                x.Rules.ContainsKey("somerule") && x.Rules["somerule"].Level == RuleLevel.Off && x.Rules.ContainsKey("someotherrule") && x.Rules["someotherrule"].Level == RuleLevel.On));
    }

    [TestMethod]
    public void UpdateFileExclusions_UpdatesGlobalSettingsWithoutRaisingEvent()
    {
        SetupUserSettings(new GlobalAnalysisSettings());
        var testSubject = CreateAndInitializeTestSubject();
        string[] exclusions = ["1", "two", "3"];

        testSubject.UpdateFileExclusions(exclusions);

        globalSettingsStorage.Received(1).SaveSettingsFile(Arg.Is<GlobalAnalysisSettings>(x => x.UserDefinedFileExclusions.SequenceEqual(exclusions, default)));
    }

    private GlobalSettingsProvider CreateAndInitializeTestSubject()
    {
        processorFactory = MockableInitializationProcessor.CreateFactory<GlobalSettingsProvider>(threadHandling, testLogger);
        var testSubject = new GlobalSettingsProvider(globalSettingsStorage, userSettingsProvider, processorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private void SetupUserSettings(GlobalAnalysisSettings globalAnalysisSettings)
    {
        var analysisSettings = new AnalysisSettings(rules: globalAnalysisSettings.Rules, globalFileExclusions: globalAnalysisSettings.UserDefinedFileExclusions);
        userSettingsProvider.UserSettings.Returns(new UserSettings(analysisSettings, string.Empty));
    }
}
