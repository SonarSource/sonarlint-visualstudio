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

using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Configuration;

[TestClass]
public class SlCoreLanguageProviderTests
{
    private SLCoreLanguageProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new SLCoreLanguageProvider();
    }

    [TestMethod]
    public void StandaloneLanguages_ShouldBeExpected()
    {
        var expected = new[] { Language.JS, Language.TS, Language.CSS, Language.HTML, Language.C, Language.CPP, Language.CS, Language.VBNET, Language.SECRETS };

        var actual = testSubject.LanguagesInStandaloneMode;

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void ExtraLanguagesInConnectedMode_ShouldBeExpected()
    {
        var expected = new[] { Language.TSQL };

        var actual = testSubject.ExtraLanguagesInConnectedMode;

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void LanguagesWithDisabledAnalysis_ShouldBeExpected()
    {
        var expected = new[] { Language.CS, Language.VBNET };

        var actual = testSubject.LanguagesWithDisabledAnalysis;

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void AllAnalyzableLanguages_ShouldBeExpected()
    {
        var expected = new[] { Language.JS, Language.TS, Language.HTML, Language.CSS, Language.C, Language.CPP, Language.SECRETS, Language.TSQL };

        var actual = testSubject.AllAnalyzableLanguages;

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void Verify_AllConfiguredLanguagesAreKnown()
    {
        var languages = testSubject.LanguagesInStandaloneMode
            .Concat(testSubject.LanguagesWithDisabledAnalysis)
            .Select(x => x.ConvertToCoreLanguage());

        languages.Should().NotContain(VisualStudio.Core.Language.Unknown);
    }

    [TestMethod]
    public void Verify_AllConfiguredLanguagesHaveKnownPluginKeys()
    {
        var languages = testSubject.LanguagesInStandaloneMode
            .Concat(testSubject.LanguagesWithDisabledAnalysis)
            .Select(x => x.GetPluginKey());

        languages.Should().NotContainNulls();
    }
}
