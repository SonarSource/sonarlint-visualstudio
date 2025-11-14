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
        private readonly RepoInfo repoInfo = new("repoKey");

        [TestMethod]
        public void Language_Ctor_ArgChecks()
        {
            // Arrange
            var key = "k";
            var name = "MyName";
            var serverLanguageKey = "serverLanguageKey";

            // Act + Assert
            // Nulls
            Action act = () => new Language(name, null, serverLanguageKey, pluginInfo, repoInfo);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("name");

            act = () => new Language(null, key, serverLanguageKey, pluginInfo, repoInfo);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("id");

            act = () => new Language(name, key, null, pluginInfo, repoInfo);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serverLanguageKey");

            act = () => new Language(name, key, serverLanguageKey, null, repoInfo, repoInfo);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("pluginInfo");

            act = () => new Language(name, key, serverLanguageKey, pluginInfo, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("repoInfo");

            act = () => new Language(name, key, serverLanguageKey, pluginInfo, repoInfo, securityRepoInfo: null);
            act.Should().NotThrow<ArgumentNullException>();

            act = () => new Language(name, key, serverLanguageKey, pluginInfo, repoInfo, additionalPlugins: null);
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
            var lang1a = new Language("Language 1", "lang1", "a", pluginInfo, repoInfo);
            var lang1b = new Language("Language 1", "lang1 XXX", "b", pluginInfo, repoInfo);
            var lang2 = new Language("Language 2", "lang2", "c", pluginInfo, repoInfo);

            // Act + Assert
            lang1b.Should().Be(lang1a, "Languages with the same ids should be equal");
            lang2.Should().NotBe(lang1a, "Languages with different ids should NOT be equal");
        }

        [TestMethod]
        public void Language_HasExpectedRepoInfo()
        {
            LanguageHasExpectedRepoInfo(Language.CSharp, "csharpsquid", "csharp");
            LanguageHasExpectedRepoInfo(Language.VBNET, "vbnet", "vbnet");
            LanguageHasExpectedRepoInfo(Language.Cpp, "cpp", "cpp");
            LanguageHasExpectedRepoInfo(Language.C, "c", "c");
            LanguageHasExpectedRepoInfo(Language.Js, "javascript", "javascript");
            LanguageHasExpectedRepoInfo(Language.Ts, "typescript", "typescript");
            LanguageHasExpectedRepoInfo(Language.Css, "css", "css");
            LanguageHasExpectedRepoInfo(Language.Html, "Web", "html");
            LanguageHasExpectedRepoInfo(Language.Secrets, "secrets", "secrets");
            LanguageHasExpectedRepoInfo(Language.Text, "text", "text");
            LanguageHasExpectedRepoInfo(Language.TSql, "tsql", "tsql");

            DoesNotHaveRepoInfo(Language.Unknown.RepoInfo);
        }

        [TestMethod]
        public void Language_HasExpectedSecurityRepoInfo()
        {
            LanguageHasExpectedSecurityRepoInfo(Language.CSharp, "roslyn.sonaranalyzer.security.cs", "csharp");
            LanguageHasExpectedSecurityRepoInfo(Language.Js, "jssecurity", "javascript");
            LanguageHasExpectedSecurityRepoInfo(Language.Ts, "tssecurity", "typescript");

            DoesNotHaveRepoInfo(Language.VBNET.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.Cpp.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.C.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.Css.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.Html.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.Secrets.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.Text.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.TSql.SecurityRepoInfo);
            DoesNotHaveRepoInfo(Language.Unknown.SecurityRepoInfo);
        }

        [TestMethod]
        [DataRow("keyToCheck", "securityKey")]
        [DataRow("repoKey", "keyToCheck")]
        public void HasRepoKey_RepoOrSecurityRepoHasKey_ReturnsTrue(string repoKey, string securityRepoKey)
        {
            var language = new Language("xxx", "dummy language", "LanguageX", pluginInfo, new RepoInfo(repoKey), new RepoInfo(securityRepoKey));

            language.HasRepoKey("keyToCheck").Should().BeTrue();
        }

        [TestMethod]
        public void HasRepoKey_RepoNorSecurityRepoHasKey_ReturnsFalse()
        {
            var language = new Language("xxx", "dummy language", "LanguageX", pluginInfo, new RepoInfo("myRepoKey"), new RepoInfo("mySecurityRepoKey"));

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

        private static void LanguageHasExpectedRepoInfo(Language language, string repoKey, string folderName) => HasExpectedRepoInfo(language.RepoInfo, repoKey, folderName);

        private static void LanguageHasExpectedSecurityRepoInfo(Language language, string repoKey, string folderName) => HasExpectedRepoInfo(language.SecurityRepoInfo, repoKey, folderName);

        private static void DoesNotHaveRepoInfo(RepoInfo repoInfo) => repoInfo.Should().BeNull();

        private static void HasExpectedRepoInfo(RepoInfo repoInfo, string repoKey, string folderName)
        {
            repoInfo.Should().NotBeNull();
            repoInfo.Key.Should().Be(repoKey);
            repoInfo.FolderName.Should().Be(folderName);
        }

        private static void LanguageHasExpectedServerLanguageKey(Language language, string key) => language.ServerLanguageKey.Should().Be(key);
    }
}
