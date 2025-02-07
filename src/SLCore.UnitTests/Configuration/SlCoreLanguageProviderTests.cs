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
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Configuration;

[TestClass]
public class SlCoreLanguageProviderTests
{
    private SLCoreLanguageProvider testSubject;
    private ILanguageProvider languageProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        MockLanguageProvider();
        testSubject = new SLCoreLanguageProvider(languageProvider);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<SLCoreLanguageProvider, ISLCoreLanguageProvider>(MefTestHelpers.CreateExport<ILanguageProvider>(languageProvider));

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SLCoreLanguageProvider>();

    [TestMethod]
    public void StandaloneLanguages_ShouldBeExpected()
    {
        _ = languageProvider.Received(1).LanguagesInStandaloneMode;
        testSubject.LanguagesInStandaloneMode.Should().BeEquivalentTo(languageProvider.LanguagesInStandaloneMode.Select(x => x.ConvertToSlCoreLanguage()));
    }

    [TestMethod]
    public void ExtraLanguagesInConnectedMode_ShouldBeExpected()
    {
        _ = languageProvider.Received(1).ExtraLanguagesInConnectedMode;
        testSubject.ExtraLanguagesInConnectedMode.Should().BeEquivalentTo(languageProvider.ExtraLanguagesInConnectedMode.Select(x => x.ConvertToSlCoreLanguage()));
    }

    [TestMethod]
    public void LanguagesWithDisabledAnalysis_ShouldBeExpected()
    {
        _ = languageProvider.Received(1).RoslynLanguages;
        testSubject.LanguagesWithDisabledAnalysis.Should().BeEquivalentTo(languageProvider.RoslynLanguages.Select(x => x.ConvertToSlCoreLanguage()));
    }

    [TestMethod]
    public void AllAnalyzableLanguages_ShouldBeExpected()
    {
        var expected = testSubject.LanguagesInStandaloneMode.Concat(testSubject.ExtraLanguagesInConnectedMode).Except(testSubject.LanguagesWithDisabledAnalysis);

        testSubject.AllAnalyzableLanguages.Should().BeEquivalentTo(expected);
    }

    private void MockLanguageProvider()
    {
        languageProvider = Substitute.For<ILanguageProvider>();
        // it doesn't have to be the real lists, just need to be different so the test can verify that the provider is using them
        languageProvider.AllKnownLanguages.Returns([Language.C, Language.Js, Language.Ts, Language.TSql]);
        languageProvider.RoslynLanguages.Returns([Language.C]);
        languageProvider.NonRoslynLanguages.Returns([Language.Js]);
        languageProvider.LanguagesInStandaloneMode.Returns([Language.Ts]);
        languageProvider.ExtraLanguagesInConnectedMode.Returns([Language.TSql]);
    }
}
