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

using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class LanguageTests
    {
        [TestMethod]
        public void Language_Ctor_ArgChecks()
        {
            // Arrange
            var key = "k";
            var name = "MyName";
            var fileSuffix = "suffix";
            var repoInfos = new RepoInfo("repoKey");
            var serverLanguage = new SonarQubeLanguage("serverKey", "serverName");
            RepoInfo defaultRepo = default;

            // Act + Assert
            // Nulls
            Action act = () => new Language(name, null, fileSuffix, serverLanguage, repoInfos);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("name");

            act = () => new Language(null, key, fileSuffix, serverLanguage, repoInfos);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("id");

            act = () => new Language(name, key, fileSuffix, null, repoInfos);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serverLanguage");

            act = () => new Language(name, key, fileSuffix, serverLanguage, defaultRepo);
            act.Should().ThrowExactly<ArgumentException>().And.Message.Should().Be("repoInfo");

            act = () => new Language(name, key, fileSuffix, serverLanguage, repoInfos, securityRepoInfo: null);
            act.Should().NotThrow<ArgumentException>();

            act = () => new Language(name, key, fileSuffix, serverLanguage, repoInfos, defaultRepo);
            act.Should().ThrowExactly<ArgumentException>().And.Message.Should().Be("securityRepoInfo");
        }

        [TestMethod]
        public void Language_UnknownLanguage()
        {
            Language.Unknown.Id.Should().BeEmpty();
            Language.Unknown.Name.Should().Be(CoreStrings.UnknownLanguageName);
        }

        [TestMethod]
        public void Language_IsSupported_SupportedLanguage_IsTrue()
        {
            // Act + Assert
            Language.CSharp.IsSupported.Should().BeTrue();
            Language.VBNET.IsSupported.Should().BeTrue();
            Language.Cpp.IsSupported.Should().BeTrue();
            Language.C.IsSupported.Should().BeTrue();
            Language.Js.IsSupported.Should().BeTrue();
            Language.Ts.IsSupported.Should().BeTrue();
            Language.Css.IsSupported.Should().BeTrue();
            Language.Secrets.IsSupported.Should().BeTrue();
            Language.Html.IsSupported.Should().BeTrue();
            Language.TSql.IsSupported.Should().BeTrue();
        }

        [TestMethod]
        public void Language_ISupported_UnsupportedLanguage_IsFalse()
        {
            var other = new Language("foo", "Foo language", "file_suffix", new SonarQubeLanguage("server key", "server name"), new RepoInfo("repoKey"));
            other.IsSupported.Should().BeFalse();
        }

        [TestMethod]
        public void Language_Equality()
        {
            // Arrange
            var lang1a = new Language("Language 1", "lang1", "file_suffix", new SonarQubeLanguage("a", "b"), new RepoInfo("repoKey"));
            var lang1b = new Language("Language 1", "lang1 XXX", "file_suffix XXX", new SonarQubeLanguage("c", "d"), new RepoInfo("repoKey"));
            var lang2 = new Language("Language 2", "lang2", "file_suffix", new SonarQubeLanguage("e", "f"), new RepoInfo("repoKey"));

            // Act + Assert
            lang1b.Should().Be(lang1a, "Languages with the same ids should be equal");
            lang2.Should().NotBe(lang1a, "Languages with different ids should NOT be equal");
        }

        [TestMethod]
        public void GetLanguageFromLanguageKey_GetsCorrectLanguage()
        {
            var cs = Language.GetLanguageFromLanguageKey("cs");
            var vbnet = Language.GetLanguageFromLanguageKey("vbnet");
            var cpp = Language.GetLanguageFromLanguageKey("cpp");
            var c = Language.GetLanguageFromLanguageKey("c");
            var js = Language.GetLanguageFromLanguageKey("js");
            var ts = Language.GetLanguageFromLanguageKey("ts");
            var css = Language.GetLanguageFromLanguageKey("css");
            var html = Language.GetLanguageFromLanguageKey("Web");
            var tsql = Language.GetLanguageFromLanguageKey("tsql");
            var unknown = Language.GetLanguageFromLanguageKey("unknown");

            cs.Should().Be(Language.CSharp);
            vbnet.Should().Be(Language.VBNET);
            cpp.Should().Be(Language.Cpp);
            c.Should().Be(Language.C);
            js.Should().Be(Language.Js);
            ts.Should().Be(Language.Ts);
            css.Should().Be(Language.Css);
            html.Should().Be(Language.Html);
            tsql.Should().Be(Language.TSql);
            unknown.Should().Be(null);
        }

        [TestMethod]
        public void GetLanguageFromRepositoryKey_GetsCorrectLanguage()
        {
            var cs = Language.GetLanguageFromRepositoryKey("csharpsquid");
            var vbnet = Language.GetLanguageFromRepositoryKey("vbnet");
            var cpp = Language.GetLanguageFromRepositoryKey("cpp");
            var c = Language.GetLanguageFromRepositoryKey("c");
            var js = Language.GetLanguageFromRepositoryKey("javascript");
            var ts = Language.GetLanguageFromRepositoryKey("typescript");
            var css = Language.GetLanguageFromRepositoryKey("css");
            var html = Language.GetLanguageFromRepositoryKey("Web");
            var secrets = Language.GetLanguageFromRepositoryKey("secrets");
            var tsql = Language.GetLanguageFromRepositoryKey("tsql");
            var unknown = Language.GetLanguageFromRepositoryKey("unknown");

            var csSecurity = Language.GetLanguageFromRepositoryKey("roslyn.sonaranalyzer.security.cs");
            var jsSecurity = Language.GetLanguageFromRepositoryKey("jssecurity");
            var tsSecurity = Language.GetLanguageFromRepositoryKey("tssecurity");

            cs.Should().Be(Language.CSharp);
            vbnet.Should().Be(Language.VBNET);
            cpp.Should().Be(Language.Cpp);
            c.Should().Be(Language.C);
            js.Should().Be(Language.Js);
            ts.Should().Be(Language.Ts);
            css.Should().Be(Language.Css);
            html.Should().Be(Language.Html);
            secrets.Should().Be(Language.Secrets);
            tsql.Should().Be(Language.TSql);
            unknown.Should().Be(null);

            csSecurity.Should().Be(Language.CSharp);
            jsSecurity.Should().Be(Language.Js);
            tsSecurity.Should().Be(Language.Ts);
        }

        [TestMethod]
        public void GetSonarRepoKeyFromLanguageKey_GetsCorrectRepoKey()
        {
            Language.GetSonarRepoKeyFromLanguage(Language.CSharp).Should().Be("csharpsquid");
            Language.GetSonarRepoKeyFromLanguage(Language.VBNET).Should().Be("vbnet");
            Language.GetSonarRepoKeyFromLanguage(Language.C).Should().Be("c");
            Language.GetSonarRepoKeyFromLanguage(Language.Cpp).Should().Be("cpp");
            Language.GetSonarRepoKeyFromLanguage(Language.Js).Should().Be("javascript");
            Language.GetSonarRepoKeyFromLanguage(Language.Ts).Should().Be("typescript");
            Language.GetSonarRepoKeyFromLanguage(Language.Css).Should().Be("css");
            Language.GetSonarRepoKeyFromLanguage(Language.Html).Should().Be("Web");
            Language.GetSonarRepoKeyFromLanguage(Language.TSql).Should().Be("tsql");

            Language.GetSonarRepoKeyFromLanguage(Language.Unknown).Should().BeNull();

            var language = new Language("xxx", "dummy language", "x", new SonarQubeLanguage("xxx", "LanguageX"), new RepoInfo("repoKey"));
            Language.GetSonarRepoKeyFromLanguage(language).Should().BeNull();
        }

        [TestMethod]
        public void SanityCheck_RoundTripFromLanguageToRepoKey_AndBack()
        {
            // Sanity check that we've remembered to define the necessary mappings
            // for all known languages.
            // Regression test to avoid bugs like #3973.

            foreach (var item in Language.KnownLanguages)
            {
                var actualRepoKey = Language.GetSonarRepoKeyFromLanguage(item);
                actualRepoKey.Should().NotBeNull();

                var actualLanguage = Language.GetLanguageFromRepositoryKey(actualRepoKey);
                actualLanguage.Should().BeSameAs(item);
            }
        }
    }
}
