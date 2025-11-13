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

using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests;

[TestClass]
public class LanguageProviderTests
{
    private LanguageProvider testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new LanguageProvider();

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<LanguageProvider, ILanguageProvider>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<LanguageProvider>();

    [TestMethod]
    public void AllKnownLanguages_ShouldBeExpected()
    {
        var expected = new[] { Language.CSharp, Language.VBNET, Language.C, Language.Cpp, Language.Js, Language.Ts, Language.Css, Language.Secrets, Language.Html, Language.TSql, Language.Text };

        testSubject.AllKnownLanguages.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void RoslynLanguages_ShouldBeExpected()
    {
        var expected = new[] { Language.CSharp, Language.VBNET };

        testSubject.RoslynLanguages.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void NonRoslynLanguages_ShouldBeExpected()
    {
        var expected = new[] { Language.C, Language.Cpp, Language.Js, Language.Ts, Language.Css, Language.Secrets, Language.Html, Language.TSql, Language.Text };

        testSubject.NonRoslynLanguages.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void LanguagesInStandaloneMode_ShouldBeExpected()
    {
        var expected = new[] { Language.CSharp, Language.VBNET, Language.C, Language.Cpp, Language.Js, Language.Ts, Language.Css, Language.Secrets, Language.Html };

        testSubject.LanguagesInStandaloneMode.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void ExtraLanguagesInConnectedMode_ShouldBeExpected()
    {
        var expected = new[] { Language.TSql, Language.Text };

        testSubject.ExtraLanguagesInConnectedMode.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void GetLanguageFromLanguageKey_GetsCorrectLanguage()
    {
        var cs = testSubject.GetLanguageFromLanguageKey("cs");
        var vbnet = testSubject.GetLanguageFromLanguageKey("vbnet");
        var cpp = testSubject.GetLanguageFromLanguageKey("cpp");
        var c = testSubject.GetLanguageFromLanguageKey("c");
        var js = testSubject.GetLanguageFromLanguageKey("js");
        var ts = testSubject.GetLanguageFromLanguageKey("ts");
        var css = testSubject.GetLanguageFromLanguageKey("css");
        var html = testSubject.GetLanguageFromLanguageKey("Web");
        var tsql = testSubject.GetLanguageFromLanguageKey("tsql");
        var text = testSubject.GetLanguageFromLanguageKey("text");
        var unknown = testSubject.GetLanguageFromLanguageKey("unknown");

        cs.Should().Be(Language.CSharp);
        vbnet.Should().Be(Language.VBNET);
        cpp.Should().Be(Language.Cpp);
        c.Should().Be(Language.C);
        js.Should().Be(Language.Js);
        ts.Should().Be(Language.Ts);
        css.Should().Be(Language.Css);
        html.Should().Be(Language.Html);
        tsql.Should().Be(Language.TSql);
        text.Should().Be(Language.Text);
        unknown.Should().Be(null);
    }

    [TestMethod]
    public void Verify_ConnectedAndStandaloneLanguagesAreKnown()
    {
        var connectedAndStandaloneLanguages = testSubject.LanguagesInStandaloneMode
            .Union(testSubject.ExtraLanguagesInConnectedMode).ToList();

        connectedAndStandaloneLanguages.Should().BeEquivalentTo(testSubject.AllKnownLanguages);
        connectedAndStandaloneLanguages.Should().NotContain(Language.Unknown);
    }

    [TestMethod]
    public void Verify_RoslynAndSlCoreLanguagesAreKnown()
    {
        var roslynAndSlCore = testSubject.RoslynLanguages
            .Union(testSubject.NonRoslynLanguages).ToList();

        roslynAndSlCore.Should().BeEquivalentTo(testSubject.AllKnownLanguages);
        roslynAndSlCore.Should().NotContain(Language.Unknown);
    }

    [TestMethod]
    public void Verify_AllConfiguredLanguagesHaveKnownPluginKeys()
    {
        testSubject.AllKnownLanguages.Should().NotContainNulls();
        testSubject.AllKnownLanguages.Should().NotContain(Language.Unknown);
    }
}
