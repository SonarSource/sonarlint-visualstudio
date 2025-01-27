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

using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.SLCore;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.Integration.Vsix.UnitTests.SLCore
{
    [TestClass]
    public class SLCoreEmbeddedPluginJarLocatorTests
    {
        private SLCoreEmbeddedPluginJarLocator testSubject;
        private IVsixRootLocator vsixRootLocator;
        private IFileSystem fileSystem;
        private ILogger logger;
        private IDirectory directory;

        [TestInitialize]
        public void TestInitialize()
        {
            vsixRootLocator = Substitute.For<IVsixRootLocator>();
            fileSystem = Substitute.For<IFileSystem>();
            logger = Substitute.For<ILogger>();
            directory = Substitute.For<IDirectory>();
            testSubject  = new SLCoreEmbeddedPluginJarLocator(vsixRootLocator, fileSystem, logger);

            MockVsixLocator();
            MockFileSystem();
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SLCoreEmbeddedPluginJarLocator, ISLCoreEmbeddedPluginJarLocator>(
                MefTestHelpers.CreateExport<IVsixRootLocator>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SLCoreEmbeddedPluginJarLocator>();
        }

        [TestMethod]
        public void ListJarFiles_DirectoryNotExists_ReturnsEmpty()
        {
            MockFileSystem(false);

            var result = testSubject.ListJarFiles();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ListJarFiles_DirectoryExists_NoFiles_ReturnsEmpty()
        {
            MockFileSystem(true);

            var result = testSubject.ListJarFiles();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ListJarFiles_FilesExist_ReturnsFiles()
        {
            MockFileSystem(true, "File1", "File2", "File3", "File4");

            var result = testSubject.ListJarFiles();

            result.Should().HaveCount(4);
            result.Should().HaveElementAt(0, "File1");
            result.Should().HaveElementAt(1, "File2");
            result.Should().HaveElementAt(2, "File3");
            result.Should().HaveElementAt(3, "File4");
        }

        [TestMethod]
        public void ListConnectedModeEmbeddedPluginPathsByKey_JarsExists_ContainsEntryForSecretsAndJavaScriptPlugin()
        {
            MockFileSystem(true,
                BuildJarFullPath("sonar-text-plugin-2.15.0.3845.jar"),
                BuildJarFullPath("sonar-javascript-plugin-10.14.0.26080.jar"),
                BuildJarFullPath("sonar-html-plugin-3.18.0.5605.jar"),
                BuildJarFullPath("sonar-cfamily-plugin-6.58.0.74356.jar"),
                BuildJarFullPath("sonar-csharp-plugin-6.58.0.74356.jar")
                );

            var result = testSubject.ListConnectedModeEmbeddedPluginPathsByKey();

            result.Count.Should().Be(4);
            VerifyContainsPlugin(result, "text", "sonar-text-plugin-2.15.0.3845.jar");
            VerifyContainsPlugin(result, "web", "sonar-html-plugin-3.18.0.5605.jar");
            VerifyContainsPlugin(result, "javascript", "sonar-javascript-plugin-10.14.0.26080.jar");
            VerifyContainsPlugin(result, "cpp", "sonar-cfamily-plugin-6.58.0.74356.jar");
        }

        [TestMethod]
        [DataRow("text", "sonar-text-plugin-2.15.0.3845.jar", "sonar-text-plugin-2.16.0.4008.jar")]
        [DataRow("javascript", "sonar-javascript-plugin-10.14.0.26080.jar", "sonar-javascript-plugin-10.15.0.0080.jar")]
        [DataRow("web", "sonar-html-plugin-10.14.0.26080.jar", "sonar-html-plugin-10.15.0.0080.jar")]
        [DataRow("cpp", "sonar-cfamily-plugin-10.14.0.26080.jar", "sonar-cfamily-plugin-10.15.0.0080.jar")]
        public void ListConnectedModeEmbeddedPluginPathsByKey_MultiplePluginVersionsExist_ReturnsTheFirstOneAndLogs(string pluginKey, string version1, string version2)
        {
            MockFileSystem(true, BuildJarFullPath(version1), BuildJarFullPath(version2));

            var result = testSubject.ListConnectedModeEmbeddedPluginPathsByKey();

            VerifyContainsPlugin(result, pluginKey, version1);
            logger.Received(1).LogVerbose(Strings.ConnectedModeEmbeddedPluginJarLocator_MultipleJars, pluginKey);
        }

        [TestMethod]
        [DataRow("text", "sonar-text-plugin-2.15.0.3845.jar", "sonar-text-plugin-enterprise-2.15.0.3845.jar")]
        [DataRow("text", "sonar-text-plugin-2.15.0.3845.jar", "sonar-html-plugin-2.15.0.3845.jar")]
        [DataRow("javascript", "sonar-javascript-plugin-10.14.0.26080.jar", "sonar-javascript-plugin-enterprise-10.14.0.26080.jar")]
        [DataRow("javascript", "sonar-javascript-plugin-10.14.0.26080.jar", "sonar-cfamily-plugin-10.14.0.26080.jar")]
        [DataRow("web", "sonar-html-plugin-10.14.0.26080.jar", "sonar-html-plugin-enterprise-10.14.0.26080.jar")]
        [DataRow("web", "sonar-html-plugin-10.14.0.26080.jar", "sonar-text-plugin-10.14.0.26080.jar")]
        [DataRow("cpp", "sonar-cfamily-plugin-10.14.0.26080.jar", "sonar-cfamily-plugin-enterprise-10.14.0.26080.jar")]
        [DataRow("cpp", "sonar-cfamily-plugin-10.14.0.26080.jar", "sonar-javascript-plugin-10.14.0.26080.jar")]
        public void ListConnectedModeEmbeddedPluginPathsByKey_PluginsWithDifferentNameExists_ReturnsCorrectOne(string pluginKey, string correct, string wrong)
        {
            MockFileSystem(true, BuildJarFullPath(wrong), BuildJarFullPath(correct));

            var result = testSubject.ListConnectedModeEmbeddedPluginPathsByKey();

            VerifyContainsPlugin(result, pluginKey, correct);
        }

        [TestMethod]
        public void ListConnectedModeEmbeddedPluginPathsByKey_NoJars_ReturnsEmptyDictionaryAndLogs()
        {
            MockFileSystem(true);

            var result = testSubject.ListConnectedModeEmbeddedPluginPathsByKey();

            result.Should().BeEmpty();
            logger.Received(4).LogVerbose(Strings.ConnectedModeEmbeddedPluginJarLocator_JarsNotFound);
        }

        private void MockFileSystem(bool directoryExists, params string[] files)
        {
            directory.Exists(default).ReturnsForAnyArgs(false);
            directory.Exists("C:\\VsixRoot\\DownloadedJars").Returns(directoryExists);

            if (directoryExists)
            {
                directory.GetFiles("C:\\VsixRoot\\DownloadedJars", "*.jar").Returns(files);
            }
        }

        private void MockFileSystem()
        {
            fileSystem.Directory.Returns(directory);
        }

        private void MockVsixLocator()
        {
            vsixRootLocator.GetVsixRoot().Returns("C:\\VsixRoot");
        }

        private static string BuildJarFullPath(string jarFileName)
        {
            return Path.Combine("C:\\VsixRoot", jarFileName);
        }

        private static void VerifyContainsPlugin(Dictionary<string, string> result, string pluginKey, string pluginFileName)
        {
            result.Keys.Should().Contain(pluginKey);
            result[pluginKey].Should().Contain(pluginFileName);
        }
    }
}
