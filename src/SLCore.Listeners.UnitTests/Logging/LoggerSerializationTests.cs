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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Listener.Logger;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Logging
{
    [TestClass]
    public class LoggerSerializationTests
    {
        [TestMethod]
        [DataRow(0, LogLevel.ERROR)]
        [DataRow(1, LogLevel.WARN)]
        [DataRow(2, LogLevel.INFO)]
        [DataRow(3, LogLevel.DEBUG)]
        [DataRow(4, LogLevel.TRACE)]
        public void DeSerializeLogParams_IntegerEnums(int level, LogLevel expectedLevel)
        {
            var jsonString = $"{{\"message\":\"Some Message\",\"level\":{level}}}";

            var result = JsonConvert.DeserializeObject<LogParams>(jsonString);

            result.message.Should().Be("Some Message");
            result.level.Should().Be(expectedLevel);
        }

        [TestMethod]
        [DataRow("ERROR", LogLevel.ERROR)]
        [DataRow("WARN", LogLevel.WARN)]
        [DataRow("INFO", LogLevel.INFO)]
        [DataRow("DEBUG", LogLevel.DEBUG)]
        [DataRow("TRACE", LogLevel.TRACE)]
        public void DeSerializeLogParams_StringEnums(string level, LogLevel expectedLevel)
        {
            var jsonString = $"{{\"message\":\"Some Message\",\"level\":\"{level}\"}}";

            var result = JsonConvert.DeserializeObject<LogParams>(jsonString);

            result.message.Should().Be("Some Message");
            result.level.Should().Be(expectedLevel);
        }

        [TestMethod]
        public void DeserializeExtraProperties()
        {
            const string serialzied =
                """
                {
                  "level": "ERROR",
                  "message": "Unable to load plugin ...\\storageRoot\\68747470733a2f2f736f6e6172636c6f75642e696f2f6f7267616e697a6174696f6e732f64756e63616e702d736f6e61722d74657374\\plugins\\sonarlint-license-plugin-8.0.0.58632-all.jar",
                  "configScopeId": "SLVS_Bound_VS2019",
                  "threadName": "SonarLint Server RPC request executor",
                  "loggerName": "sonarlint",
                  "stackTrace": "java.lang.IllegalStateException: Error while reading plugin manifest from jar: ...\\storageRoot\\68747470733a2f2f736f6e6172636c6f75642e696f2f6f7267616e697a6174696f6e732f64756e63616e702d736f6e61722d74657374\\plugins\\sonarlint-license-plugin-8.0.0.58632-all.jar\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.SonarPluginManifest.fromJar(SonarPluginManifest.java:105)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.PluginInfo.create(PluginInfo.java:221)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.SonarPluginRequirementsChecker.checkRequirements(SonarPluginRequirementsChecker.java:64)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.PluginsLoader.load(PluginsLoader.java:65)\r\n\tat org.sonarsource.sonarlint.core.plugin.PluginsService.loadPlugins(PluginsService.java:189)\r\n\tat org.sonarsource.sonarlint.core.plugin.PluginsService.loadPlugins(PluginsService.java:147)\r\n\tat org.sonarsource.sonarlint.core.plugin.PluginsService.getPlugins(PluginsService.java:136)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.lambda$getOrCreateConnectedEngine$2(AnalysisEngineCache.java:97)\r\n\tat java.base/java.util.concurrent.ConcurrentHashMap.computeIfAbsent(Unknown Source)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.getOrCreateConnectedEngine(AnalysisEngineCache.java:96)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.lambda$getOrCreateAnalysisEngine$1(AnalysisEngineCache.java:91)\r\n\tat java.base/java.util.Optional.map(Unknown Source)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.getOrCreateAnalysisEngine(AnalysisEngineCache.java:91)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisService.analyze(AnalysisService.java:647)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AnalysisRpcServiceDelegate.lambda$analyzeFilesAndTrack$8(AnalysisRpcServiceDelegate.java:143)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AbstractRpcServiceDelegate.lambda$requestAsync$0(AbstractRpcServiceDelegate.java:67)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AbstractRpcServiceDelegate.computeWithLogger(AbstractRpcServiceDelegate.java:135)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AbstractRpcServiceDelegate.lambda$requestAsync$1(AbstractRpcServiceDelegate.java:65)\r\n\tat java.base/java.util.concurrent.CompletableFuture$UniApply.tryFire(Unknown Source)\r\n\tat java.base/java.util.concurrent.CompletableFuture$Completion.run(Unknown Source)\r\n\tat java.base/java.util.concurrent.ThreadPoolExecutor.runWorker(Unknown Source)\r\n\tat java.base/java.util.concurrent.ThreadPoolExecutor$Worker.run(Unknown Source)\r\n\tat java.base/java.lang.Thread.run(Unknown Source)\r\nCaused by: java.nio.file.NoSuchFileException: C:\\Users\\georgii.borovinskikh\\AppData\\Local\\SLVS_SLOOP\\storageRoot\\68747470733a2f2f736f6e6172636c6f75642e696f2f6f7267616e697a6174696f6e732f64756e63616e702d736f6e61722d74657374\\plugins\\sonarlint-license-plugin-8.0.0.58632-all.jar\r\n\tat java.base/sun.nio.fs.WindowsException.translateToIOException(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsException.rethrowAsIOException(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsException.rethrowAsIOException(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsFileAttributeViews$Basic.readAttributes(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsFileAttributeViews$Basic.readAttributes(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsFileSystemProvider.readAttributes(Unknown Source)\r\n\tat java.base/java.nio.file.Files.readAttributes(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile$Source.get(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile$CleanableResource.<init>(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile.<init>(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile.<init>(Unknown Source)\r\n\tat java.base/java.util.jar.JarFile.<init>(Unknown Source)\r\n\tat java.base/java.util.jar.JarFile.<init>(Unknown Source)\r\n\tat java.base/java.util.jar.JarFile.<init>(Unknown Source)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.SonarPluginManifest.fromJar(SonarPluginManifest.java:97)\r\n\t... 22 more\r\n",
                  "loggedAt": 1734968894643
                }
                """;
            var expected = new LogParams
            {
                message = "Unable to load plugin ...\\storageRoot\\68747470733a2f2f736f6e6172636c6f75642e696f2f6f7267616e697a6174696f6e732f64756e63616e702d736f6e61722d74657374\\plugins\\sonarlint-license-plugin-8.0.0.58632-all.jar",
                level = LogLevel.ERROR,
                configScopeId = "SLVS_Bound_VS2019",
                threadName = "SonarLint Server RPC request executor",
                loggerName = "sonarlint",
                stackTrace = "java.lang.IllegalStateException: Error while reading plugin manifest from jar: ...\\storageRoot\\68747470733a2f2f736f6e6172636c6f75642e696f2f6f7267616e697a6174696f6e732f64756e63616e702d736f6e61722d74657374\\plugins\\sonarlint-license-plugin-8.0.0.58632-all.jar\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.SonarPluginManifest.fromJar(SonarPluginManifest.java:105)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.PluginInfo.create(PluginInfo.java:221)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.SonarPluginRequirementsChecker.checkRequirements(SonarPluginRequirementsChecker.java:64)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.PluginsLoader.load(PluginsLoader.java:65)\r\n\tat org.sonarsource.sonarlint.core.plugin.PluginsService.loadPlugins(PluginsService.java:189)\r\n\tat org.sonarsource.sonarlint.core.plugin.PluginsService.loadPlugins(PluginsService.java:147)\r\n\tat org.sonarsource.sonarlint.core.plugin.PluginsService.getPlugins(PluginsService.java:136)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.lambda$getOrCreateConnectedEngine$2(AnalysisEngineCache.java:97)\r\n\tat java.base/java.util.concurrent.ConcurrentHashMap.computeIfAbsent(Unknown Source)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.getOrCreateConnectedEngine(AnalysisEngineCache.java:96)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.lambda$getOrCreateAnalysisEngine$1(AnalysisEngineCache.java:91)\r\n\tat java.base/java.util.Optional.map(Unknown Source)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisEngineCache.getOrCreateAnalysisEngine(AnalysisEngineCache.java:91)\r\n\tat org.sonarsource.sonarlint.core.analysis.AnalysisService.analyze(AnalysisService.java:647)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AnalysisRpcServiceDelegate.lambda$analyzeFilesAndTrack$8(AnalysisRpcServiceDelegate.java:143)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AbstractRpcServiceDelegate.lambda$requestAsync$0(AbstractRpcServiceDelegate.java:67)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AbstractRpcServiceDelegate.computeWithLogger(AbstractRpcServiceDelegate.java:135)\r\n\tat org.sonarsource.sonarlint.core.rpc.impl.AbstractRpcServiceDelegate.lambda$requestAsync$1(AbstractRpcServiceDelegate.java:65)\r\n\tat java.base/java.util.concurrent.CompletableFuture$UniApply.tryFire(Unknown Source)\r\n\tat java.base/java.util.concurrent.CompletableFuture$Completion.run(Unknown Source)\r\n\tat java.base/java.util.concurrent.ThreadPoolExecutor.runWorker(Unknown Source)\r\n\tat java.base/java.util.concurrent.ThreadPoolExecutor$Worker.run(Unknown Source)\r\n\tat java.base/java.lang.Thread.run(Unknown Source)\r\nCaused by: java.nio.file.NoSuchFileException: C:\\Users\\georgii.borovinskikh\\AppData\\Local\\SLVS_SLOOP\\storageRoot\\68747470733a2f2f736f6e6172636c6f75642e696f2f6f7267616e697a6174696f6e732f64756e63616e702d736f6e61722d74657374\\plugins\\sonarlint-license-plugin-8.0.0.58632-all.jar\r\n\tat java.base/sun.nio.fs.WindowsException.translateToIOException(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsException.rethrowAsIOException(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsException.rethrowAsIOException(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsFileAttributeViews$Basic.readAttributes(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsFileAttributeViews$Basic.readAttributes(Unknown Source)\r\n\tat java.base/sun.nio.fs.WindowsFileSystemProvider.readAttributes(Unknown Source)\r\n\tat java.base/java.nio.file.Files.readAttributes(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile$Source.get(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile$CleanableResource.<init>(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile.<init>(Unknown Source)\r\n\tat java.base/java.util.zip.ZipFile.<init>(Unknown Source)\r\n\tat java.base/java.util.jar.JarFile.<init>(Unknown Source)\r\n\tat java.base/java.util.jar.JarFile.<init>(Unknown Source)\r\n\tat java.base/java.util.jar.JarFile.<init>(Unknown Source)\r\n\tat org.sonarsource.sonarlint.core.plugin.commons.loading.SonarPluginManifest.fromJar(SonarPluginManifest.java:97)\r\n\t... 22 more\r\n"
            };

            JsonConvert.DeserializeObject<LogParams>(serialzied).Should().BeEquivalentTo(expected, options => options.ComparingByMembers<LogParams>());
        }
    }
}
