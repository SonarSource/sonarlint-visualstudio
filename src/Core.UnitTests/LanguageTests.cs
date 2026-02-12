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

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class LanguageTests
    {
        private readonly PluginInfo pluginInfo = new("pluginKey", "filePattern");
        private const string RepoKey = "repoKey";

        [TestMethod]
        public void Language_Ctor_ArgChecks()
        {
            // Arrange
            var key = "k";
            var name = "MyName";
            var serverLanguageKey = "serverLanguageKey";

            // Act + Assert
            // Nulls
            Action act = () => new Language(name, null, serverLanguageKey, pluginInfo, RepoKey);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("name");

            act = () => new Language(null, key, serverLanguageKey, pluginInfo, RepoKey);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("id");

            act = () => new Language(name, key, null, pluginInfo, RepoKey);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serverLanguageKey");

            act = () => new Language(name, key, serverLanguageKey, null, RepoKey, RepoKey);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("pluginInfo");

            act = () => new Language(name, key, serverLanguageKey, pluginInfo, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("repoKey");

            act = () => new Language(name, key, serverLanguageKey, pluginInfo, RepoKey, securityRepoKey: null);
            act.Should().NotThrow<ArgumentNullException>();

            act = () => new Language(name, key, serverLanguageKey, pluginInfo, RepoKey, additionalPlugins: null);
            act.Should().NotThrow<ArgumentNullException>();
        }

        [TestMethod]
        public void Language_UnknownLanguage()
        {
            Language.Unknown.Id.Should().BeEmpty();
            Language.Unknown.Name.Should().Be(CoreStrings.UnknownLanguageName);
        }

        [TestMethod]
        public void Language_Equality()
        {
            // Arrange
            var lang1a = new Language("Language 1", "lang1", "a", pluginInfo, RepoKey);
            var lang1b = new Language("Language 1", "lang1 XXX", "b", pluginInfo, RepoKey);
            var lang2 = new Language("Language 2", "lang2", "c", pluginInfo, RepoKey);

            // Act + Assert
            lang1b.Should().Be(lang1a, "Languages with the same ids should be equal");
            lang2.Should().NotBe(lang1a, "Languages with different ids should NOT be equal");
        }

        [TestMethod]
        public void Language_HasExpectedRepoKey()
        {
            Language.CSharp.RepoKey.Should().Be("csharpsquid");
            Language.VBNET.RepoKey.Should().Be("vbnet");
            Language.Cpp.RepoKey.Should().Be("cpp");
            Language.C.RepoKey.Should().Be("c");
            Language.Js.RepoKey.Should().Be("javascript");
            Language.Ts.RepoKey.Should().Be("typescript");
            Language.Css.RepoKey.Should().Be("css");
            Language.Html.RepoKey.Should().Be("Web");
            Language.Secrets.RepoKey.Should().Be("secrets");
            Language.Text.RepoKey.Should().Be("text");
            Language.TSql.RepoKey.Should().Be("tsql");

            Language.Unknown.RepoKey.Should().BeNull();
        }

        [TestMethod]
        public void Language_HasExpectedSecurityRepoKey()
        {
            Language.CSharp.SecurityRepoKey.Should().Be("roslyn.sonaranalyzer.security.cs");
            Language.Js.SecurityRepoKey.Should().Be("jssecurity");
            Language.Ts.SecurityRepoKey.Should().Be("tssecurity");

            Language.VBNET.SecurityRepoKey.Should().BeNull();
            Language.Cpp.SecurityRepoKey.Should().BeNull();
            Language.C.SecurityRepoKey.Should().BeNull();
            Language.Css.SecurityRepoKey.Should().BeNull();
            Language.Html.SecurityRepoKey.Should().BeNull();
            Language.Secrets.SecurityRepoKey.Should().BeNull();
            Language.Text.SecurityRepoKey.Should().BeNull();
            Language.TSql.SecurityRepoKey.Should().BeNull();
            Language.Unknown.SecurityRepoKey.Should().BeNull();
        }

        [TestMethod]
        [DataRow("keyToCheck", "securityKey")]
        [DataRow("repoKey", "keyToCheck")]
        public void HasRepoKey_RepoOrSecurityRepoHasKey_ReturnsTrue(string repoKey, string securityRepoKey)
        {
            var language = new Language("xxx", "dummy language", "LanguageX", pluginInfo, repoKey, securityRepoKey);

            language.HasRepoKey("keyToCheck").Should().BeTrue();
        }

        [TestMethod]
        public void HasRepoKey_RepoNorSecurityRepoHasKey_ReturnsFalse()
        {
            var language = new Language("xxx", "dummy language", "LanguageX", pluginInfo, "myRepoKey", "mySecurityRepoKey");

            language.HasRepoKey("keyToCheck").Should().BeFalse();
        }

        [TestMethod]
        public void Language_HasCorrectPlugin()
        {
            LanguageHasExpectedPlugin(Language.CSharp, "sqvsroslyn", "sonarqube-ide-visualstudio-roslyn-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");
            LanguageHasExpectedAdditionalPlugins(Language.CSharp, [new("csharpenterprise", "sonar-csharp-enterprise-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar", isEnabledForAnalysis: false)]);
            LanguageHasExpectedPlugin(Language.VBNET, "sqvsroslyn", "sonarqube-ide-visualstudio-roslyn-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");
            LanguageHasExpectedAdditionalPlugins(Language.VBNET, [new("vbnetenterprise", "sonar-vbnet-enterprise-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar", isEnabledForAnalysis: false)]);

            LanguageHasExpectedPlugin(Language.Cpp, "cpp", "sonar-cfamily-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");
            LanguageHasExpectedPlugin(Language.C, "cpp", "sonar-cfamily-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");

            LanguageHasExpectedPlugin(Language.Js, "javascript", "sonar-javascript-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");
            LanguageHasExpectedPlugin(Language.Ts, "javascript", "sonar-javascript-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");
            LanguageHasExpectedPlugin(Language.Css, "javascript", "sonar-javascript-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");

            LanguageHasExpectedPlugin(Language.Secrets, "text", "sonar-text-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");
            LanguageHasExpectedPlugin(Language.Text, "text", "sonar-text-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");

            LanguageHasExpectedPlugin(Language.Html, "web", "sonar-html-plugin-(\\d+\\.\\d+\\.\\d+\\.\\d+)\\.jar");

            LanguageHasExpectedPlugin(Language.TSql, "tsql", null);
        }

        [TestMethod]
        public void Language_HasExpectedServerLanguageKey()
        {
            LanguageHasExpectedServerLanguageKey(Language.CSharp, "cs");
            LanguageHasExpectedServerLanguageKey(Language.VBNET, "vbnet");
            LanguageHasExpectedServerLanguageKey(Language.Cpp, "cpp");
            LanguageHasExpectedServerLanguageKey(Language.C, "c");
            LanguageHasExpectedServerLanguageKey(Language.Js, "js");
            LanguageHasExpectedServerLanguageKey(Language.Ts, "ts");
            LanguageHasExpectedServerLanguageKey(Language.Css, "css");
            LanguageHasExpectedServerLanguageKey(Language.Html, "web");
            LanguageHasExpectedServerLanguageKey(Language.Secrets, "secrets");
            LanguageHasExpectedServerLanguageKey(Language.Text, "text");
            LanguageHasExpectedServerLanguageKey(Language.TSql, "tsql");
        }

        private static void LanguageHasExpectedPlugin(Language language, string pluginKey, string filePattern)
        {
            language.PluginInfo.Key.Should().Be(pluginKey);
            language.PluginInfo.FilePattern.Should().Be(filePattern);
            language.PluginInfo.IsEnabledForAnalysis.Should().Be(true);
        }

        private static void LanguageHasExpectedAdditionalPlugins(Language language, List<PluginInfo> expectedPlugins)
        {
            foreach (var expectedPlugin in expectedPlugins)
            {
                language.AdditionalPlugins.Should().Contain(x => x.Key == expectedPlugin.Key &&
                                                                 x.FilePattern == expectedPlugin.FilePattern &&
                                                                 x.IsEnabledForAnalysis == expectedPlugin.IsEnabledForAnalysis);
            }
        }

        private static void LanguageHasExpectedServerLanguageKey(Language language, string key) => language.ServerLanguageKey.Should().Be(key);
    }
}
