/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var serverLanguage = new SonarQubeLanguage("serverKey", "serverName");

            // Act + Assert
            // Nulls
            Action act = () => new Language(name, null, fileSuffix, serverLanguage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("name");

            act = () => new Language(null, key, fileSuffix, serverLanguage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("id");

            act = () => new Language(name, key, null, serverLanguage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSuffix");

            act = () => new Language(name, key, fileSuffix, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serverLanguage");
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
            Language.Secrets.IsSupported.Should().BeTrue();
        }

        [TestMethod]
        public void Language_ISupported_UnsupportedLanguage_IsFalse()
        {
            var other = new Language("foo", "Foo language", "file_suffix", new SonarQubeLanguage("server key", "server name"));
            other.IsSupported.Should().BeFalse();
        }

        [TestMethod]
        public void Language_ServerLanguageObjectsAndKeys()
        {
            // Act + Assert
            Language.CSharp.ServerLanguage.Key.Should().Be(SonarLanguageKeys.CSharp);
            Language.VBNET.ServerLanguage.Key.Should().Be(SonarLanguageKeys.VBNet);
            Language.Cpp.ServerLanguage.Key.Should().Be(SonarLanguageKeys.CPlusPlus);
            Language.C.ServerLanguage.Key.Should().Be(SonarLanguageKeys.C);
            Language.Js.ServerLanguage.Key.Should().Be(SonarLanguageKeys.JavaScript);
            Language.Ts.ServerLanguage.Key.Should().Be(SonarLanguageKeys.TypeScript);
            Language.Secrets.ServerLanguage.Key.Should().Be(SonarLanguageKeys.Secrets);
            Language.Unknown.ServerLanguage.Should().BeNull();
        }

        [TestMethod]
        public void Language_Equality()
        {
            // Arrange
            var lang1a = new Language("Language 1", "lang1", "file_suffix", new SonarQubeLanguage("a", "b"));
            var lang1b = new Language("Language 1", "lang1 XXX", "file_suffix XXX", new SonarQubeLanguage("c", "d"));
            var lang2 = new Language("Language 2", "lang2", "file_suffix", new SonarQubeLanguage("e", "f"));

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
            var unknown = Language.GetLanguageFromLanguageKey("unknown");

            cs.Should().Be(Language.CSharp);
            vbnet.Should().Be(Language.VBNET);
            cpp.Should().Be(Language.Cpp);
            c.Should().Be(Language.C);
            js.Should().Be(Language.Js);
            ts.Should().Be(Language.Ts);
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
            var secrets = Language.GetLanguageFromRepositoryKey("secrets");
            var unknown = Language.GetLanguageFromRepositoryKey("unknown");

            cs.Should().Be(Language.CSharp);
            vbnet.Should().Be(Language.VBNET);
            cpp.Should().Be(Language.Cpp);
            c.Should().Be(Language.C);
            js.Should().Be(Language.Js);
            ts.Should().Be(Language.Ts);
            secrets.Should().Be(Language.Secrets);
            unknown.Should().Be(null);
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

            Language.GetSonarRepoKeyFromLanguage(Language.Unknown).Should().BeNull();

            var language = new Language("xxx", "dummy language", "x", new SonarQubeLanguage("xxx", "LanguageX"));
            Language.GetSonarRepoKeyFromLanguage(language).Should().BeNull();
        }

        [TestMethod]
        public void SanityCheck_RoundTripFromLanguageToRepoKey_AndBack()
        {
            // Sanity check that we've remembered to define the necessary mappings
            // for all known languages.
            // Regression test to avoid bugs like #3973.

            foreach(var item in Language.KnownLanguages)
            {
                var actualRepoKey = Language.GetSonarRepoKeyFromLanguage(item);
                actualRepoKey.Should().NotBeNull();

                var actualLanguage = Language.GetLanguageFromRepositoryKey(actualRepoKey);
                actualLanguage.Should().BeSameAs(item);
            }
        }
    }
}
