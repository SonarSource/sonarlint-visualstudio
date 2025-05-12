﻿/*
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
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB.StandaloneMode;

[TestClass]
public class StandaloneRoslynSettingsUpdaterTests
{
    private IRoslynConfigGenerator roslynConfigGenerator;
    private StandaloneRoslynSettingsUpdater testSubject;
    private ILanguageProvider languageProvider;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        roslynConfigGenerator = Substitute.For<IRoslynConfigGenerator>();
        languageProvider = Substitute.For<ILanguageProvider>();
        threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>())
            .Returns(info => info.Arg<Func<Task<int>>>()());

        testSubject = new StandaloneRoslynSettingsUpdater(
            roslynConfigGenerator,
            languageProvider,
            threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<StandaloneRoslynSettingsUpdater, IStandaloneRoslynSettingsUpdater>(
            MefTestHelpers.CreateExport<IRoslynConfigGenerator>(),
            MefTestHelpers.CreateExport<ILanguageProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<StandaloneRoslynSettingsUpdater>();

    [TestMethod]
    public void Update_CallsGeneratorWithCorrectLanguageAndDirectory()
    {
        IReadOnlyList<Language> fakeRoslynLanguages = [Language.VBNET, Language.TSql, Language.C];
        languageProvider.RoslynLanguages.Returns(fakeRoslynLanguages);
        var userSettings = new UserSettings(new AnalysisSettings(), @"APPDATA\SonarLint for Visual Studio\.global");

        testSubject.Update(userSettings);

        Received.InOrder(() =>
        {
            foreach (var language in fakeRoslynLanguages)
            {
                roslynConfigGenerator.GenerateAndSaveConfiguration(
                    language,
                    @"APPDATA\SonarLint for Visual Studio\.global",
                    Arg.Is<IDictionary<string, string>>(x => x.Count == 0),
                    Arg.Any<IFileExclusions>(),
                    Arg.Is<IReadOnlyCollection<IRoslynRuleStatus>>(x => x.Count == 0),
                    Arg.Is<IReadOnlyCollection<IRuleParameters>>(x => x.Count == 0));
            }
        });
    }

    [TestMethod]
    public void Update_CallsGeneratorWithCorrectProperties()
    {
        IReadOnlyList<Language> fakeRoslynLanguages = [Language.VBNET, Language.TSql, Language.C];
        languageProvider.RoslynLanguages.Returns(fakeRoslynLanguages);
        var properties = ImmutableDictionary.Create<string, string>().SetItem("key", "value");
        var userSettings = new UserSettings(new AnalysisSettings(analysisProperties:  properties), "any");

        testSubject.Update(userSettings);

        Received.InOrder(() =>
        {
            foreach (var language in fakeRoslynLanguages)
            {
                roslynConfigGenerator.GenerateAndSaveConfiguration(
                    language,
                    Arg.Any<string>(),
                    properties,
                    Arg.Any<IFileExclusions>(),
                    Arg.Is<IReadOnlyCollection<IRoslynRuleStatus>>(x => x.Count == 0),
                    Arg.Is<IReadOnlyCollection<IRuleParameters>>(x => x.Count == 0));
            }
        });
    }

    [TestMethod]
    public void Update_ConvertsExclusionsCorrectly()
    {
        IReadOnlyList<Language> fakeRoslynLanguages = [Language.VBNET, Language.TSql, Language.C];
        languageProvider.RoslynLanguages.Returns(fakeRoslynLanguages);

        testSubject.Update(new UserSettings(new AnalysisSettings([], ["one", "two"]), "any"));

        Received.InOrder(() =>
        {
            foreach (var language in fakeRoslynLanguages)
            {
                roslynConfigGenerator.GenerateAndSaveConfiguration(
                    language,
                    Arg.Any<string>(),
                    Arg.Is<IDictionary<string, string>>(x => x.Count == 0),
                    Arg.Is<StandaloneRoslynFileExclusions>(x => x.ToDictionary()["sonar.exclusions"] == "**/one,**/two"),
                    Arg.Is<IReadOnlyCollection<IRoslynRuleStatus>>(x => x.Count == 0),
                    Arg.Is<IReadOnlyCollection<IRuleParameters>>(x => x.Count == 0));
            }
        });
    }

    [TestMethod]
    public void Update_ConvertsRulesCorrectly()
    {
        IReadOnlyList<Language> fakeRoslynLanguages = [Language.VBNET];
        languageProvider.RoslynLanguages.Returns(fakeRoslynLanguages);
        var rules = new Dictionary<string, RuleConfig>()
        {
            { "vbnet:S1", new RuleConfig(RuleLevel.On, new Dictionary<string, string> { { "1", "11" } }) },
            { "vbnet:S2", new RuleConfig(RuleLevel.Off, []) },
            { "vbnet:S3", new RuleConfig(RuleLevel.On, []) },
            { "vbnet:S4", new RuleConfig(RuleLevel.Off, new Dictionary<string, string> { { "4", "44" } }) },
        };

        testSubject.Update(new UserSettings(new AnalysisSettings(rules, []), "any"));

        roslynConfigGenerator
            .Received()
            .GenerateAndSaveConfiguration(
                Language.VBNET,
                Arg.Any<string>(),
                Arg.Is<IDictionary<string, string>>(x => x.Count == 0),
                Arg.Any<StandaloneRoslynFileExclusions>(),
                Arg.Is<IReadOnlyCollection<IRoslynRuleStatus>>(x => x.Count == 4),
                Arg.Is<IReadOnlyCollection<IRuleParameters>>(x => x.Count == 4));
        var statuses = roslynConfigGenerator.ReceivedCalls().Single().GetArguments()[4] as List<IRoslynRuleStatus>;
        statuses.Should().BeEquivalentTo(new List<StandaloneRoslynRuleStatus>
        {
            new(new SonarCompositeRuleId("vbnet", "S1"), true),
            new(new SonarCompositeRuleId("vbnet", "S2"), false),
            new(new SonarCompositeRuleId("vbnet", "S3"), true),
            new(new SonarCompositeRuleId("vbnet", "S4"), false),
        });
        var parameters = roslynConfigGenerator.ReceivedCalls().Single().GetArguments()[5] as List<IRuleParameters>;
        parameters.Should().BeEquivalentTo(new List<StandaloneRoslynRuleParameters>
        {
            new(new SonarCompositeRuleId("vbnet", "S1"), new Dictionary<string, string> { { "1", "11" } }),
            new(new SonarCompositeRuleId("vbnet", "S2"), new Dictionary<string, string>()),
            new(new SonarCompositeRuleId("vbnet", "S3"), new Dictionary<string, string>()),
            new(new SonarCompositeRuleId("vbnet", "S4"), new Dictionary<string, string> { { "4", "44" } }),
        });
    }

    [TestMethod]
    public void Update_GroupsRulesByLanguage()
    {
        IReadOnlyList<Language> fakeRoslynLanguages = [Language.VBNET, Language.CSharp];
        languageProvider.RoslynLanguages.Returns(fakeRoslynLanguages);
        var rules = new Dictionary<string, RuleConfig>()
        {
            { "vbnet:S1", new RuleConfig(default) }, { "vbnet:S2", new RuleConfig(default) }, { "csharpsquid:S3", new RuleConfig(default) }, { "cpp:S4", new RuleConfig(default) },
        };

        testSubject.Update(new UserSettings(new AnalysisSettings(rules, []), "any"));

        Received.InOrder(() =>
        {
            roslynConfigGenerator
                .GenerateAndSaveConfiguration(
                    Language.VBNET,
                    Arg.Any<string>(),
                    Arg.Is<IDictionary<string, string>>(x => x.Count == 0),
                    Arg.Any<StandaloneRoslynFileExclusions>(),
                    Arg.Is<IReadOnlyCollection<IRoslynRuleStatus>>(x => x.Count == 2),
                    Arg.Is<IReadOnlyCollection<IRuleParameters>>(x => x.Count == 2));
            roslynConfigGenerator
                .GenerateAndSaveConfiguration(
                    Language.CSharp,
                    Arg.Any<string>(),
                    Arg.Is<IDictionary<string, string>>(x => x.Count == 0),
                    Arg.Any<StandaloneRoslynFileExclusions>(),
                    Arg.Is<IReadOnlyCollection<IRoslynRuleStatus>>(x => x.Count == 1),
                    Arg.Is<IReadOnlyCollection<IRuleParameters>>(x => x.Count == 1));
        });
        roslynConfigGenerator
            .DidNotReceive()
            .GenerateAndSaveConfiguration(
                Language.Cpp,
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(),
                Arg.Any<IFileExclusions>(),
                Arg.Any<IReadOnlyCollection<IRoslynRuleStatus>>(),
                Arg.Any<IReadOnlyCollection<IRuleParameters>>());
    }
}
